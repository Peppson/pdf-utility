using Serilog;
using WpfApp1.Models;

namespace WpfApp1.Config;

public class UserConfig
{
    public UserConfig() => Reset();

    public void Reset()
    {
        SelectedFont = iText.IO.Font.Constants.StandardFonts.HELVETICA; // Default
        FontSize = 14;
        IsBoldFont = false;
        IsItalicFont = false;
        OnlyRotatePages = false;
        ProcessingDisabled = false;

        // Header
        Header_Enabled = true;
        Header_LeftTextMode = TextMode.Dynamic;
        Header_LeftText = "L Header";
        Header_LeftCount = 100_000;
        Header_RightTextMode = TextMode.Static;
        Header_RightText = "R Header";
        Header_RightCount = 100_000;

        // Footer
        Footer_Enabled = true;
        Footer_LeftTextMode = TextMode.Static;
        Footer_LeftText = "L Footer";
        Footer_LeftCount = 100_000;
        Footer_RightTextMode = TextMode.Dynamic;
        Footer_RightText = "R Footer";
        Footer_RightCount = 100_000;
        
        Log.Debug("UserConfig Reset()");
    }

    public string SelectedFont { get; set; }
    public int FontSize { get; set; }
    public bool IsBoldFont { get; set; }
    public bool IsItalicFont { get; set; }
    public bool OnlyRotatePages { get; set; }
    public bool ProcessingDisabled { get; set; }

    // Header
    public bool Header_Enabled { get; set; }
    public TextMode Header_LeftTextMode { get; set; }
    public string Header_LeftText { get; set; }
    public int Header_LeftCount { get; set; }
    public TextMode Header_RightTextMode { get; set; }
    public string Header_RightText { get; set; }
    public int Header_RightCount { get; set; }
    
    // Footer
    public bool Footer_Enabled { get; set; }
    public TextMode Footer_LeftTextMode { get; set; }
    public string  Footer_LeftText { get; set; }
    public int Footer_LeftCount { get; set; }
    public TextMode Footer_RightTextMode { get; set; }
    public string Footer_RightText { get; set; }
    public int Footer_RightCount { get; set; }
}
