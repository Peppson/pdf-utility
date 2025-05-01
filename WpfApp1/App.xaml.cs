using System.Windows;
using Serilog;
using WpfApp1.Config;
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

        var mainWindow = new MainWindow(pdfService);
        mainWindow.Show();
        
        /* WpfApp1.Properties.Settings.Default.IsFirstStartup = true;
        WpfApp1.Properties.Settings.Default.Save(); */

        // Show "License Agreement" window on first startup
        if (!DialogService.PromptLicenseAgreement())
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
