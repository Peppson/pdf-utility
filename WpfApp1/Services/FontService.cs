using System.ComponentModel;
using iText.IO.Font.Constants;
using iText.Layout.Font;
using Serilog;
using WpfApp1.Config;

namespace WpfApp1.Services;

public interface IFontService
{   
    iText.Kernel.Font.PdfFont GetSelectedFont();
    void PrintAvailableFonts();
    FontProvider GetFontProvider();
}

public class FontService : IFontService, INotifyPropertyChanged
{
    private readonly FontProvider _fontProvider;
    private readonly UserConfig _userConfig;
    public event PropertyChangedEventHandler? PropertyChanged;
    public FontProvider GetFontProvider() => _fontProvider;

    public static List<string> AvailableFonts { get; set; } =
    [
        "Helvetica",
        "Courier",
        "Times-Roman",
    ];

    private static readonly List<string> _fontNames =
    [
        "Courier",
        "Courier-Bold",
        "Courier-BoldOblique",
        "Courier-Oblique",
        "Helvetica",
        "Helvetica-Bold",
        "Helvetica-BoldOblique",
        "Helvetica-Oblique",
        "Times-Roman",
        "Times-Bold",
        "Times-BoldItalic",
        "Times-Italic"
    ];

    public bool IsBold
    {
        get => _userConfig.IsBoldFont;
        set
        {
            if (_userConfig.IsBoldFont != value)
            {
                _userConfig.IsBoldFont = value;
                OnPropertyChanged(nameof(IsBold));
            }
        }
    }

    public bool IsItalic
    {
        get => _userConfig.IsItalicFont;
        set
        {
            if (_userConfig.IsItalicFont != value)
            {
                _userConfig.IsItalicFont = value;
                OnPropertyChanged(nameof(IsItalic));
            }
        }
    }

    public string SelectedFont
    {
        get => _userConfig.SelectedFont;
        set
        {
            if (_userConfig.SelectedFont != value)
            {
                _userConfig.SelectedFont = value;
                OnPropertyChanged(nameof(SelectedFont));
            }
        }
    }

    public FontService(UserConfig userConfig)
    {
        _userConfig = userConfig;
        _fontProvider = new FontProvider();
        _fontProvider.AddStandardPdfFonts(); 
        // Todo add nisse font
    }
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void PrintAvailableFonts()
    {
        foreach (var font in _fontProvider.GetFontSet().GetFonts())
        {
            Log.Debug(font.GetFontName());
        }
    }

    public iText.Kernel.Font.PdfFont GetSelectedFont()
    {   
        var fontName = GetFontNameWithStyle(); // Font, Italic, Bold, etc.

        if (!_fontNames.Contains(fontName))
        {   
            string message = $"Unknown font \"{fontName}\", falling back to Helvetica.";
            DialogService.Warning(message);
            Log.Warning(message);
            fontName = StandardFonts.HELVETICA;
        }

        return iText.Kernel.Font.PdfFontFactory.CreateFont(fontName);
    }

    private string GetFontNameWithStyle()
    {      
        var baseFont = _userConfig.SelectedFont;

        if (baseFont == "Times-Roman")
            return GetTimesRomanFontName(); // Special case

        if (_userConfig.IsBoldFont && _userConfig.IsItalicFont)
            return $"{baseFont}-BoldOblique";
        else if (_userConfig.IsBoldFont)
            return $"{baseFont}-Bold";
        else if (_userConfig.IsItalicFont)
            return $"{baseFont}-Oblique";
        else
            return baseFont;
    }

    // Why is Times Roman different from the rest?
    private string GetTimesRomanFontName()
    {
        if (_userConfig.IsBoldFont && _userConfig.IsItalicFont)
            return "Times-BoldItalic";
        else if (_userConfig.IsBoldFont && !_userConfig.IsItalicFont)
            return "Times-Bold";
        else if (!_userConfig.IsBoldFont && _userConfig.IsItalicFont)
            return "Times-Italic";
        else
            return "Times-Roman";
    }
}
