using iText.IO.Font.Constants;
using iText.Layout.Font;
using Serilog;

namespace WpfApp1.Services;

public class FontService
{
    private readonly FontProvider _fontProvider;


    /*
        Courier
        Courier-Bold
        Courier-BoldOblique
        Courier-Oblique
        Helvetica
        Helvetica-Bold
        Helvetica-BoldOblique
        Helvetica-Oblique
        Symbol
        Times-Roman
        Times-Bold
        Times-BoldItalic
        Times-Italic
        ZapfDingbats
    */

    public FontService()
    {
        _fontProvider = new FontProvider();
        _fontProvider.AddStandardPdfFonts();
    }

    public void PrintAvailableFonts()
    {
        foreach (var font in _fontProvider.GetFontSet().GetFonts())
        {
            Log.Debug(font.GetFontName());
        }
    }

    public iText.Kernel.Font.PdfFont GetFont(string fontName)
    {
        switch (fontName)
        {
            case "TIMES-ROMAN":
                return iText.Kernel.Font.PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
            case "HELVETICA":
                return iText.Kernel.Font.PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            case "COURIER":
                return iText.Kernel.Font.PdfFontFactory.CreateFont(StandardFonts.COURIER);

            // Fallback
            default:
                Log.Warning("Using fallback font: HELVETICA");
                return iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
        }
    }

    public FontProvider GetFontProvider() => _fontProvider;
}
