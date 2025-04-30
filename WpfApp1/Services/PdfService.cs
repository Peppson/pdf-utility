using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Drawing.Imaging;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using WpfApp1.Helpers;
using WpfApp1.Models;
using WpfApp1.Properties;
using Tesseract;
using System.Windows.Automation.Peers;
using System.Linq;
using static iText.Layout.Font.FontProvider;
using iText.Svg.Renderers.Impl;
using iText.Kernel.Validation.Context;

namespace WpfApp1.Services;

public interface IPdfService
{
    bool SelectPdf();
    bool DragAndDropPdf(DragEventArgs e);
    List<Pdf> SelectedPdfFiles { get; }
    int FileCount => SelectedPdfFiles.Count;
    bool RemoveAllPdf();    
    void DoTheThing();
    void ResetAll();
    //public bool IsAllValidPdfExtension(string[] files);
}

public class PdfService(WinDialogService winDialogService, PdfSettings settings, FontService fontService) : IPdfService
{
    private readonly WinDialogService _winDialogService = winDialogService;
    private readonly FontService _fontService = fontService;
    private readonly PdfSettings _settings = settings;
    public List<Pdf> SelectedPdfFiles { get; private set; } = new List<Pdf>(50);
    public int FileCount => SelectedPdfFiles.Count;
    private int _PDFCounter = 0;


    public bool SelectPdf()
    {
        if (!WinDialogService.GetFilePathsFromExplorerWindow(out string[] paths))
            return false;

        return TryAddPdf(paths);
    }

    public bool DragAndDropPdf(DragEventArgs e)
    {
        if (!WinDialogService.GetFilePathsFromDragAndDrop(e, out string[] paths))
            return false;

        return TryAddPdf(paths);
    }

    private bool TryAddPdf(string[] paths)
    {
        if (paths.Length == 0) return false;

        // Add new PDFs
        var corruptedFiles = new List<string>();
        foreach (var path in paths)
        {
            AddPdf(path, corruptedFiles);
        }
        // Any corrupted files?
        if (corruptedFiles.Count > 0)
            WinDialogService.ShowCorruptedFilesDialog(corruptedFiles);

        return true;
    }

    private void AddPdf(string path, List<string> corruptedPdfFiles)
    {
        try
        {   
            // Check for valid pdf
            PdfReader reader = new(path);
            PdfDocument readDoc = new(reader);
            Pdf pdf = new(File.ReadAllBytes(path), path);

            // File already exists?
            if (SelectedPdfFiles.Any(doc => doc.FilePath == path))
            {
                // Add the file anyway?
                if (!WinDialogService.PromptAddExistingFile(path))
                {
                    return;
                }
            }

            SelectedPdfFiles.Add(pdf);
        }
        catch (Exception)
        {
            corruptedPdfFiles.Add(Path.GetFileName(path));
        }
    }

    public bool RemoveAllPdf()
    {
        if (SelectedPdfFiles.Count == 0) return false;

        SelectedPdfFiles.Clear();
        return true;
    }


    
    


    

    public void DoTheThing()
    {
        using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        string outputPath = GetOutputPath();
        string indexFileContent = "";
        _PDFCounter = 0; // Make damn sure

        foreach (var pdf in SelectedPdfFiles)
        {   
            try
            {   
                ProcessPdf(pdf, engine, outputPath, ref indexFileContent);
            }
            catch (Exception ex)
            {   
                WinDialogService.ErrorProcessingPDF(ex);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace); // todo
                ResetAll();
                break;
            }
        }

        OutputIndexFile(indexFileContent, outputPath);
        _PDFCounter = 0;
        Console.WriteLine("Done!"); //todo
    }






    private void ProcessPdf(Pdf pdf, TesseractEngine engine, string outputPath, ref string indexFileContent)
    {   
        int fileName = 1000_00 + _PDFCounter;
        string outputFilePath = $"{outputPath}{fileName}.pdf";

        // Tesseract image engine for detecting page rotations
        var pageRotations = GetPageRotations(pdf, engine);

        // Build pdf doc
        using var stream = new MemoryStream(pdf.RawBytes);
        using var reader = new PdfReader(stream);
        using var writer = new PdfWriter(outputFilePath);
        using var pdfDoc = new PdfDocument(reader, writer);

        // Working but ugly todo
        using var stream2 = new MemoryStream(pdf.RawBytes);
        using var reader2 = new PdfReader(stream2);
        using var pdfTEST = new PdfDocument(reader2);

        //using var document = new Document(pdfDoc);

        // Add content?
        if (_settings.Header_Enabled || _settings.Footer_Enabled)
            AddHeaderFooter(pdfDoc, pageRotations, pdfTEST);
        else
            RotatePages(pdfDoc, pageRotations);

        // Write to index file and save
        indexFileContent += $"{fileName}.pdf - {pdf.FileName}\n";
        _PDFCounter++;
        //document.Close(); // todo
    }

    private static int[] GetPageRotations(Pdf pdf, TesseractEngine engine)
    {
        const int widthPx = 2480;
        const int heightPx = 3508;
        const int dpi = 300;

        // Load pdf
        using var stream = new MemoryStream(pdf.RawBytes);
        using PdfiumViewer.PdfDocument document = PdfiumViewer.PdfDocument.Load(stream); 
        var pageRotations = new int[document.PageCount];

        // Get page rotations
        for (int i = 0; i < document.PageCount; i++)
        {
            // "Printscreen" current page to bitmap
            using var png = document.Render(i, widthPx, heightPx, dpi, dpi, true);
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

            Console.WriteLine($"Page {i + 1} rot: {orientation}\t\t{confidence:0.##} confidence");
            pageRotations[i] = orientation;
        }

        return pageRotations;
    }

    private static int CalculatePageRotation(int rotation)
    {
        if (rotation != 0)
            return (rotation + 180) % 360;
        
        return 0;
    }

    private void AddHeaderFooter(PdfDocument pdf, int[] pageRotations, PdfDocument pdfTEST)
    {   
        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
        {   
            var rotation = pageRotations[i - 1];
            var page = pdf.GetPage(i);
            var width = page.GetPageSize().GetWidth();
            var height = page.GetPageSize().GetHeight();

            // Rotate current page if needed
            RotatePage(rotation, page);

            // Shrink page content to fit header/footer
            ScaleOriginalContent(page, width, height, i, pdfTEST); // todo pagenumber
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

    private void ScaleOriginalContent(PdfPage page, float width, float height, int pageNumber = 999, PdfDocument? pdfTEST = null)
    {
        var scaleFactor = _settings.ScaleFactor;
        //var pageCopy = page.CopyAsFormXObject(page.GetDocument());



        var pageTest = pdfTEST.GetPage(pageNumber);

        PdfFormXObject? pageCopy = null;
        try
        {
            //pageCopy = page.CopyAsFormXObject(page.GetDocument());

            pageCopy = pageTest.CopyAsFormXObject(page.GetDocument());
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"P: {pageNumber}   - Error {ex.Message} ");
            Console.WriteLine(ex.StackTrace); // todo

            return;
        }


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

    private void ScaleAnnotations(PdfPage page, float width, float height)
    {
        var annotations = page.GetAnnotations();
        float offsetX = (width - width * _settings.ScaleFactor) / 2;
        float offsetY = (height - height * _settings.ScaleFactor) / 2;

        foreach (var annotation in annotations)
        {   
            var rect = annotation.GetRectangle().ToRectangle();

            var scaledRect = new iText.Kernel.Geom.Rectangle(
                rect.GetX() * _settings.ScaleFactor + offsetX,
                rect.GetY() * _settings.ScaleFactor + offsetY,
                rect.GetWidth() * _settings.ScaleFactor,
                rect.GetHeight() * _settings.ScaleFactor
            );

            // Highlighted text
            if (annotation.GetSubtype().Equals(PdfName.Highlight))
            {
                ScaleHighlightedText(page, scaledRect, annotation);
            }
            else // All other
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

    private static void ScaleHighlightedText(
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
        if (!_settings.Header_Enabled) return;
        var table = new Table(2, true);

        // Left text
        if (_settings.Header_LeftTextMode != TextMode.Off)
        {
            string leftText = (_settings.Header_LeftTextMode == TextMode.Static) ?
                _settings.Header_LeftText :
                $"{_settings.Header_LeftText} - {_settings.Header_LeftCount + _PDFCounter}";

            table.AddCell(CreateCell(leftText, iText.Layout.Properties.TextAlignment.LEFT));
        }
        else // Empty cell as spacer
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.LEFT));
        }

        // Right text
        if (_settings.Header_RightTextMode != TextMode.Off)
        {
            string rightText = (_settings.Header_RightTextMode == TextMode.Static) ?
                _settings.Header_RightText :
                $"{_settings.Header_RightText} - {_settings.Header_RightCount + _PDFCounter}";

            table.AddCell(CreateCell(rightText, iText.Layout.Properties.TextAlignment.RIGHT));
        }
        else
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.RIGHT));
        }

        WriteContentToPage(canvas, PositionType.Header, height, width, table);
    }

    private void AddFooter(PdfCanvas canvas, float height, float width)
    {
        if (!_settings.Footer_Enabled) return;
        var table = new Table(2, true);

        // Left text
        if (_settings.Footer_LeftTextMode != TextMode.Off)
        {
            string leftText = _settings.Footer_LeftTextMode == TextMode.Static ?
                _settings.Footer_LeftText :
                $"{_settings.Footer_LeftText} - {_settings.Footer_LeftCount + _PDFCounter}";

            table.AddCell(CreateCell(leftText, iText.Layout.Properties.TextAlignment.LEFT));
        }
        else
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.LEFT));
        }

        // Right text
        if (_settings.Footer_RightTextMode != TextMode.Off)
        {
            string rightText = _settings.Footer_RightTextMode == TextMode.Static ?
                _settings.Footer_RightText :
                $"{_settings.Footer_RightText} - {_settings.Footer_RightCount + _PDFCounter}";

            table.AddCell(CreateCell(rightText, iText.Layout.Properties.TextAlignment.RIGHT));
        }
        else
        {
            table.AddCell(CreateCell("", iText.Layout.Properties.TextAlignment.RIGHT));
        }
        
        WriteContentToPage(canvas, PositionType.Footer, height, width, table);
    }
    
    private void WriteContentToPage(PdfCanvas pdfCanvas, PositionType positionType, float height, float width, Table table) 
    {
        const float tableHeight = 100;
        const float posX = 0;

        var posY = (positionType == PositionType.Header) ?
            height - _settings.MarginTop : 
            _settings.MarginBottom;
        
        // Write
        var content = new Canvas(pdfCanvas, new iText.Kernel.Geom.Rectangle(posX, posY, width, tableHeight));
        table.SetFixedPosition(posX, posY, width);
        content.Add(table);
    }

    private static void RotatePages(PdfDocument pdf, int[] pageRotations)
    {        
        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
        {    
            var rotation = pageRotations[i - 1];
            var page = pdf.GetPage(i);

            if (rotation != 0)
            {
                page.SetRotation(0);
                page.SetRotation(CalculatePageRotation(rotation));
            }
        }
    }

    private static void RotatePage(int rotation, PdfPage page)
    {
        if (rotation != 0)
        {
            page.SetRotation(0);
            page.SetRotation(CalculatePageRotation(rotation));
        }
    }

    private Cell CreateCell(string text, iText.Layout.Properties.TextAlignment alignment)
    {   
        var font = _fontService.GetFont(_settings.Font);
        
        return new Cell()
            .Add(new Paragraph(text)

            #if HEADER_FOOTER_COLOR
                .SetBackgroundColor(ColorConstants.ORANGE)
            #endif
            
            .SetTextAlignment(alignment)
            .SetFontSize(_settings.FontSize))
            .SetFont(font)
            .SetBorder(Border.NO_BORDER)
            .SetFontColor(ColorConstants.BLACK)
            .SetPaddingLeft(10)   
            .SetPaddingRight(10);
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

    public static bool IsAllValidPdfExtension(string[] files)
    {
        if (files.Length == 0) return false;

        return files.All(file =>
            Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        );
    }    

    private string GetOutputPath()
    {
        string desktopPath = SetAndGetDesktopFolder();
        string subFolderName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string subFolderPath = Path.Combine(desktopPath, subFolderName);

        if (!Directory.Exists(subFolderPath))
        {
            Directory.CreateDirectory(subFolderPath);
            return subFolderPath + "/";
        }

        // Add "_{i}" to folder if already present, unlikely
        int counter = 1;
        string outputPath = $"{subFolderPath}_{counter}";
        
        while (Directory.Exists(outputPath))
        {
            counter++;
            outputPath = $"{subFolderPath}_{counter}";
        }

        Directory.CreateDirectory(outputPath);
        return outputPath + "/";
    }

    private string SetAndGetDesktopFolder()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (string.IsNullOrEmpty(desktopPath))
            WinDialogService.ErrorGettingDesktop(new Exception("Cannot find path."));

        // Create folder
        var outputFolder = Path.Combine(desktopPath, _settings.outputFolderName);
        if (!Directory.Exists(outputFolder))
        {
            Console.WriteLine("Creating folder: " + outputFolder);
            Directory.CreateDirectory(outputFolder);
        }

        return outputFolder;
    }

    private static void OutputIndexFile(string content, string outputPath)
    {
        string filePath = outputPath + "Index.txt";
        using var indexFile = new StreamWriter(filePath, append: true);

        indexFile.WriteLine("---- Index of processed files ----\n");
        indexFile.WriteLine(content);
    }

    public void ResetAll()
    {
        SelectedPdfFiles.Clear();
        _settings.Reset();
    }
}
