using System.Windows;
using iText.Kernel.Font;
using WpfApp1.Properties;

namespace WpfApp1.Models;

public class PdfSettings : SettingsConstants
{
    public PdfSettings() => Reset();

    public void Reset()
    {
        Font = "TIMES-ROMAN";
        FontSize = 14;
        OnlyRotatePages = false;

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
        
        Console.WriteLine("PdfSettings Reset()"); //todo
    }

    public string Font { get; set; }
    public int FontSize { get; set; }
    public bool OnlyRotatePages { get; set; }

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
