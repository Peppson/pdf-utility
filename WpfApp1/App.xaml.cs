using System.Drawing;
using System.IO;
using System.Windows;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = new PdfSettings();
        var fontService = new FontService();
        var windowService = new WinDialogService();
        var pdfService = new PdfService(windowService, settings, fontService);


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

        var mainWindow = new MainWindow(pdfService);
        mainWindow.Show();
        
        /* WpfApp1.Properties.Settings.Default.IsFirstStartup = true;
        WpfApp1.Properties.Settings.Default.Save(); */

        // Show "License Agreement" window on first startup
        if (!WinDialogService.PromptLicenseAgremeent())
        {
            Current.Shutdown(); // ¯\_(ツ)_/¯
        }
    } 

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
