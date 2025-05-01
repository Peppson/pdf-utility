using System.IO;
using System.Windows;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using Serilog;
using Tesseract;
using WpfApp1.Config;
using WpfApp1.Models;

namespace WpfApp1.Services;

public interface IPdfService
{
    bool SelectPdf();
    bool DragAndDropPdf(DragEventArgs e);
    static List<Pdf> LoadedPdfs { get; }
    int FileCount => LoadedPdfs.Count;
    void ProcessAllPdfs();
    bool RemoveAllPdfs();    
    void ResetAll();
}

public class PdfService(FontService fontService, UserConfig userConfig) : IPdfService
{
    private readonly FontService _fontService = fontService;
    private readonly UserConfig _userConfig = userConfig;
    public static List<Pdf> LoadedPdfs { get; private set; } = new List<Pdf>(50);
    public static List<(int Pdf, int Page)> LowConfidencePages { get; private set; } = [];
    public int FileCount => LoadedPdfs.Count;
    private int _PDFCounter = 0;


    public bool SelectPdf()
    {
        if (!DialogService.GetFilePathsExplorerWindow(out string[] paths))
            return false;

        return TryAddPdf(paths);
    }

    public bool DragAndDropPdf(DragEventArgs e)
    {
        if (!DialogService.GetFilePathsDragAndDrop(e, out string[] paths))
            return false;

        return TryAddPdf(paths);
    }

    private static bool TryAddPdf(string[] paths)
    {
        if (paths.Length == 0) return false;

        // Add new PDFs
        var corruptedFiles = new List<string>(5);
        foreach (var path in paths)
        {
            AddPdf(path, corruptedFiles);
        }
        // Any corrupted files?
        if (corruptedFiles.Count > 0)
            DialogService.ShowCorruptedFilesDialog(corruptedFiles);

        return true;
    }

    private static void AddPdf(string path, List<string> corruptedPdfs)
    {
        try
        {   
            // Check for valid pdf
            PdfReader reader = new(path);
            PdfDocument readDoc = new(reader);
            Pdf pdf = new(File.ReadAllBytes(path), path);

            // Pdf already exists?
            if (LoadedPdfs.Any(x => x.FilePath == path))
            {
                // Add anyway?
                if (!DialogService.PromptAddExistingFile(path))
                    return;
            }
            LoadedPdfs.Add(pdf);
        }
        catch (Exception)
        {
            corruptedPdfs.Add(Path.GetFileName(path));
        }
    }

    public void ProcessAllPdfs()
    {
        if (IsProcessingDisabled()) return;

        using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        var outputPath = FileOutputService.GetOutputPath();
        var indexFileContent = "";
        var hasFailed = false;
        _PDFCounter = 0;

        foreach (var pdf in LoadedPdfs)
        {   
            try
            {   
                ProcessPdf(pdf, engine, outputPath, ref indexFileContent);
            }
            catch (Exception ex)
            {   
                Log.Warning("Error processing PDF: " + ex.Message);
                DialogService.ErrorProcessingPDF(ex);
                hasFailed = true;
            }
        }

        // Cleanup if failed
        if (hasFailed)
        {
            DeleteFailedOutput(outputPath);
            return;
        }

        ClearPdfData();
        FileOutputService.OutputIndexFile(indexFileContent, outputPath);
        Log.Debug("Success!");
    }

    private void ProcessPdf(Pdf pdf, TesseractEngine engine, string outputPath, ref string indexFileContent)
    {   
        var outputFileName = AppConstants.outputFileName + _PDFCounter;
        var outputFilePath = $"{outputPath}{outputFileName}.pdf";

        // Tesseract image engine for detecting page rotations
        var pageRotations = GetPageRotations(pdf, engine);

        // Output PDF
        using var stream = new MemoryStream(pdf.RawBytes);
        using var reader = new PdfReader(stream);
        using var writer = new PdfWriter(outputFilePath);
        using var outputPdf = new PdfDocument(reader, writer);

        // PDF used for coping XObject content
        using var stream2 = new MemoryStream(pdf.RawBytes);
        using var reader2 = new PdfReader(stream2);
        using var inputPdf = new PdfDocument(reader2);

        // What are we doing?
        if (_userConfig.OnlyRotatePages)
            RotateAllPages(outputPdf, pageRotations);
        else if (_userConfig.Header_Enabled || _userConfig.Footer_Enabled)
            AddHeaderFooter(outputPdf, inputPdf, pageRotations);

        indexFileContent += $"{outputFileName}.pdf - {pdf.FileName}\n";
        _PDFCounter++;
    }

    private int[] GetPageRotations(Pdf pdf, TesseractEngine engine)
    {
        const bool forPrinting = true;
        const int widthPx = 2480;
        const int heightPx = 3508;
        const int dpi = 300;

        // Load pdf
        using var stream = new MemoryStream(pdf.RawBytes);
        using var document = PdfiumViewer.PdfDocument.Load(stream); 
        var pageRotations = new int[document.PageCount];

        // Get page rotations
        for (int i = 0; i < document.PageCount; i++)
        {
            // "Printscreen" current page to bitmap
            using var png = document.Render(i, widthPx, heightPx, dpi, dpi, forPrinting);
            using var ms = new MemoryStream();

            #pragma warning disable CA1416
                png.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            #pragma warning restore CA1416

            byte[] imageBytes = ms.ToArray();

            // Get rotation from Tesseract engine
            using var pixImg = Pix.LoadFromMemory(imageBytes);
            using var deskewedImg = pixImg.Deskew();
            using var processedImg = engine.Process(deskewedImg);
            processedImg.DetectBestOrientation(out int orientation, out float confidence);

            pageRotations[i] = orientation;
            Log.Debug($"Page {i + 1} rot: {orientation}\t\t{confidence:0.##} confidence");

            // Flag low confidence pages
            if (confidence < AppConstants.PageConfidenceThreshold)
            {
                LowConfidencePages.Add((_PDFCounter, i + 1));
                Log.Debug($"Pdf: {_PDFCounter} page {i + 1} - low confidence ({confidence:0.##})");
            }
        }

        return pageRotations;
    }

    private void AddHeaderFooter(PdfDocument outputPdf, PdfDocument inputPdf, int[] pageRotations)
    {   
        for (int i = 1; i <= outputPdf.GetNumberOfPages(); i++)
        {   
            var rotation = pageRotations[i - 1];
            var page = outputPdf.GetPage(i);
            var inputPage = inputPdf.GetPage(i);
            var width = page.GetPageSize().GetWidth();
            var height = page.GetPageSize().GetHeight();

            // Rotate page if needed
            RotatePage(rotation, page);

            // Shrink page content to fit header/footer
            ScaleOriginalContent(page, inputPage, width, height);
            ScaleAnnotations(page, width, height);

            // Get new width and height after rotation
            var rotatedWidth = page.GetPageSizeWithRotation().GetWidth();
            var rotatedHeight = page.GetPageSizeWithRotation().GetHeight();

            // Create new canvas for header/footer
            var canvas = new PdfCanvas(page);
            RotateCanvas(canvas, page.GetRotation(), rotatedWidth, rotatedHeight);

            AddHeader(canvas, rotatedHeight, rotatedWidth);
            AddFooter(canvas, rotatedHeight, rotatedWidth);
        }
    }

    private static void ScaleOriginalContent(PdfPage page, PdfPage inputpage, float width, float height)
    {
        var scaleFactor = AppConstants.ScaleFactor;
        var pageCopy = inputpage.CopyAsFormXObject(page.GetDocument());

        // Create a new canvas for the page
        var canvas = new PdfCanvas(page);
        canvas.SaveState();

        // Clear original content
        canvas.SetFillColor(ColorConstants.WHITE);
        canvas.Rectangle(0, 0, width, height);
        canvas.Fill();

        // Add new scaled content back
        canvas.ConcatMatrix(scaleFactor, 0, 0, scaleFactor, (width - width * scaleFactor) / 2, (height - height * scaleFactor) / 2);
        canvas.AddXObject(pageCopy);

        #if CONTENT_COLOR
            canvas.SetFillColor(ColorConstants.GREEN);
            canvas.Rectangle(0, 0, width, height);
            canvas.Fill();
        #endif

        canvas.RestoreState();
    }

    private static void ScaleAnnotations(PdfPage page, float width, float height)
    {
        var annotations = page.GetAnnotations();
        float offsetX = (width - width * AppConstants.ScaleFactor) / 2;
        float offsetY = (height - height * AppConstants.ScaleFactor) / 2;

        foreach (var annotation in annotations)
        {   
            var rect = annotation.GetRectangle().ToRectangle();

            var scaledRect = new iText.Kernel.Geom.Rectangle(
                rect.GetX() * AppConstants.ScaleFactor + offsetX,
                rect.GetY() * AppConstants.ScaleFactor + offsetY,
                rect.GetWidth() * AppConstants.ScaleFactor,
                rect.GetHeight() * AppConstants.ScaleFactor
            );

            // Highlighted text
            if (annotation.GetSubtype().Equals(PdfName.Highlight))
            {
                WriteNewHighlightedText(page, scaledRect, annotation);
            }
            else // Others
            {   
                annotation.SetRectangle(new PdfArray(
                [
                    scaledRect.GetX(), 
                    scaledRect.GetY(), 
                    scaledRect.GetX() + scaledRect.GetWidth(), 
                    scaledRect.GetY() + scaledRect.GetHeight() 
                ]));
            }
        }
    }

    private static void WriteNewHighlightedText(
        PdfPage page, 
        iText.Kernel.Geom.Rectangle scaledRect, 
        iText.Kernel.Pdf.Annot.PdfAnnotation annotation)
    {
        var canvas = new PdfCanvas(page);

        canvas.SaveState();
        canvas.SetFillColor(ColorConstants.ORANGE);
        canvas.SetExtGState(new iText.Kernel.Pdf.Extgstate.PdfExtGState().SetFillOpacity(0.25f));
        canvas.Rectangle(scaledRect.GetX(), scaledRect.GetY(), scaledRect.GetWidth(), scaledRect.GetHeight());
        canvas.Fill();
        canvas.RestoreState();

        page.RemoveAnnotation(annotation);
    }

    private void AddHeader(PdfCanvas canvas, float height, float width)
    {
        if (!_userConfig.Header_Enabled) return;
        var table = new Table(2, true);

        // Left text
        if (_userConfig.Header_LeftTextMode != TextMode.Off)
        {
            string leftText = (_userConfig.Header_LeftTextMode == TextMode.Static) ?
                _userConfig.Header_LeftText :
                $"{_userConfig.Header_LeftText} - {_userConfig.Header_LeftCount + _PDFCounter}";

            table.AddCell(CreateCell(leftText, iText.Layout.Properties.TextAlignment.LEFT));
        }
        else // Empty cell as spacer
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.LEFT));
        }

        // Right text
        if (_userConfig.Header_RightTextMode != TextMode.Off)
        {
            string rightText = (_userConfig.Header_RightTextMode == TextMode.Static) ?
                _userConfig.Header_RightText :
                $"{_userConfig.Header_RightText} - {_userConfig.Header_RightCount + _PDFCounter}";

            table.AddCell(CreateCell(rightText, iText.Layout.Properties.TextAlignment.RIGHT));
        }
        else
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.RIGHT));
        }

        WriteHeaderFooter(canvas, PositionType.Header, height, width, table);
    }

    private void AddFooter(PdfCanvas canvas, float height, float width)
    {
        if (!_userConfig.Footer_Enabled) return;
        var table = new Table(2, true);

        // Left text
        if (_userConfig.Footer_LeftTextMode != TextMode.Off)
        {
            string leftText = _userConfig.Footer_LeftTextMode == TextMode.Static ?
                _userConfig.Footer_LeftText :
                $"{_userConfig.Footer_LeftText} - {_userConfig.Footer_LeftCount + _PDFCounter}";

            table.AddCell(CreateCell(leftText, iText.Layout.Properties.TextAlignment.LEFT));
        }
        else
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.LEFT));
        }

        // Right text
        if (_userConfig.Footer_RightTextMode != TextMode.Off)
        {
            string rightText = _userConfig.Footer_RightTextMode == TextMode.Static ?
                _userConfig.Footer_RightText :
                $"{_userConfig.Footer_RightText} - {_userConfig.Footer_RightCount + _PDFCounter}";

            table.AddCell(CreateCell(rightText, iText.Layout.Properties.TextAlignment.RIGHT));
        }
        else
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.RIGHT));
        }

        WriteHeaderFooter(canvas, PositionType.Footer, height, width, table);
    }
    
    private static void WriteHeaderFooter(PdfCanvas pdfCanvas, PositionType positionType, float height, float width, Table table) 
    {
        const float tableHeight = 100;
        const float posX = 0;

        var posY = (positionType == PositionType.Header) ?
            height - AppConstants.MarginTop : 
            AppConstants.MarginBottom;
        
        // Write
        var content = new Canvas(pdfCanvas, new iText.Kernel.Geom.Rectangle(posX, posY, width, tableHeight));
        table.SetFixedPosition(posX, posY, width);
        content.Add(table);
    }

    private static int CalculatePageRotation(int rotation)
    {
        if (rotation != 0)
            return (rotation + 180) % 360;
        
        return 0;
    }

    private static void RotatePage(int rotation, PdfPage page)
    {
        if (rotation != 0)
        {
            page.SetRotation(0);
            page.SetRotation(CalculatePageRotation(rotation));
        }
    }

    private static void RotateAllPages(PdfDocument outputPdf, int[] pageRotations)
    {        
        Log.Debug($"Rotating {outputPdf.GetNumberOfPages()} pages...");

        for (int i = 1; i <= outputPdf.GetNumberOfPages(); i++)
        {    
            var rotation = pageRotations[i - 1];
            var page = outputPdf.GetPage(i);

            if (rotation != 0)
            {
                page.SetRotation(0);
                page.SetRotation(CalculatePageRotation(rotation));
            }
        }
    }

    private static void RotateCanvas(PdfCanvas canvas, int rotation, float width, float height)
    {   
        switch (rotation)
        {
            case 90:
                canvas.ConcatMatrix(0, 1, -1, 0, height, 0);
                break;
            case 180:
                canvas.ConcatMatrix(-1, 0, 0, -1, width, height);
                break;
            case 270:
                canvas.ConcatMatrix(0, -1, 1, 0, 0, width);
                break;
            default:
                break;
        }
    }

    private Cell CreateCell(string text, iText.Layout.Properties.TextAlignment alignment)
    {   
        var font = _fontService.GetFont(_userConfig.Font);
        
        return new Cell()
            .Add(new Paragraph(text)

            #if HEADER_FOOTER_COLOR
                .SetBackgroundColor(ColorConstants.ORANGE)
            #endif
            
            .SetTextAlignment(alignment)
            .SetFontSize(_userConfig.FontSize))
            .SetFont(font)
            .SetBorder(Border.NO_BORDER)
            .SetFontColor(ColorConstants.BLACK)
            .SetPaddingLeft(10)   
            .SetPaddingRight(10);
    }

    private static void DeleteFailedOutput(string outputPath)
    {   
        ClearPdfData();

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
            Log.Warning($"Deleting folder: {outputPath}");
        }
        else
        {
            Log.Debug($"Folder not found: {outputPath}");
        }
    }

    public static bool IsAllValidPdfExtension(string[] files)
    {
        if (files.Length == 0) return false;

        return files.All(file =>
            Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        );
    }

    private bool IsProcessingDisabled()
    {   
        bool isDisabled;
        if (!_userConfig.OnlyRotatePages && !_userConfig.Header_Enabled && !_userConfig.Footer_Enabled)
        {
            Log.Debug("No header/footer or rotation enabled, skipping...");
            isDisabled = true;
        }
        else
        {
            isDisabled = false;
        }

        _userConfig.ProcessingDisabled = isDisabled;
        return isDisabled;
    }

    private static void ClearPdfData()
    {
        LoadedPdfs.Clear();
        LowConfidencePages.Clear();
    }  

    public bool RemoveAllPdfs()
    {
        if (LoadedPdfs.Count == 0) return false;

        ClearPdfData();
        return true;
    }

    public void ResetAll()
    {
        ClearPdfData();
        _userConfig.Reset();
    }
}
