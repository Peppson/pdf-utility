using System;
using System.Drawing; // Requires System.Drawing.Common on .NET Core/5/6/7
using PdfiumViewer;
using Tesseract;

namespace dev;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        DoIt();
    }

    static void DoIt()
    {   
        string[] fileNames = ["Horizontel_page_1_90fel.pdf",  "Horizontel_page_2_270fel.pdf", "Blandad_page_2_90fel.pdf", "Vertikal_page_2.pdf"];
        const int targetWidth = 2480;  
        const int targetHeight = 3508;
        const int dpi = 300;

        using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

        foreach (var fileName in fileNames)
        {
            string pdfPath = @"C:\Users\jw\Desktop\Samples\" + fileName;            
            using var document = PdfDocument.Load(pdfPath);
            Console.WriteLine($"\nPdf: {fileName}:");

            for (int i = 0; i < document.PageCount; i++)
            {
                // "Printscreen" current pdf page to filestream bitmap
                using var pageImage = document.Render(i, targetWidth, targetHeight, dpi, dpi, true);
                using var ms = new MemoryStream();
                #pragma warning disable CA1416
                    pageImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                #pragma warning restore CA1416
                byte[] imageBytes = ms.ToArray();

                // Get orientation from Tesseract engine
                using var pixImage = Pix.LoadFromMemory(imageBytes);
                using var deskewedImg = pixImage.Deskew();
                using var currentPage = engine.Process(deskewedImg);
                currentPage.DetectBestOrientation(out int orientation, out float confidence);

                Console.WriteLine($"Page {i + 1} rot: {orientation}\t\t{confidence:0.##} confidence");
            }
        }
    }
}



// Save to disc and load image OLD
/*
for (int i = 0; i < document.PageCount; i++)
{
    using var image = document.Render(i, targetWidth, targetHeight, 300, 300, true);
    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

    using var img = Pix.LoadFromFile(outputImagePath + i + ".png");
    using var deskewedImg = img.Deskew();
    using var pdfPage = engine.Process(deskewedImg);

    pdfPage.DetectBestOrientation(out int orientation, out float confidence);
    Console.WriteLine($"Page {i + 1} rot: {orientation}\t\t{confidence:0.##} confidence");
} 
*/
