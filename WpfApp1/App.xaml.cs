using System.Drawing;
using System.IO;
using System.Windows;
using Serilog;
using WpfApp1.Config;
using WpfApp1.Models;
using WpfApp1.Services;
using WpfApp1.Views;

namespace WpfApp1;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Services
        var userConfig = new UserConfig();
        var fontService = new FontService();
        var pdfService = new PdfService(fontService, userConfig);

        // Logger
        #if DEBUG
            LogService.Init(isRelease: false);
        #else
            LogService.Init(isRelease: true);
        #endif
        Log.Information("-- Application started --");



        /*
        Log.Debug("This is a debug message");
        Log.Information("Application started");
        Log.Warning("Disk space is running low");
        Log.Error("Failed to save the file");
        Log.Fatal("Unhandled exception - shutting down");
        */

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
        if (!DialogService.PromptLicenseAgremeent())
        {
            Current.Shutdown(); // ¯\_(ツ)_/¯
        }
    } 

    protected override void OnExit(ExitEventArgs e)
    {   
        LogService.Shutdown();
        base.OnExit(e);
    }
}
