using System.IO;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using WpfApp1.Config;

namespace WpfApp1.Services;

public static class LogService
{   
    public static string LogFilePath { get; private set; } = string.Empty;


    public static void Init(bool isRelease)
    {   
        if (isRelease)   
            SetupReleaseLogger();
        else
            SetupDebugLogger();
    }

    public static void SetupReleaseLogger()
    {   
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.ApplicationName
        );
        LogFilePath = Path.Combine(logDirectory, "Log.txt");

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.File(LogFilePath, 
                rollingInterval: RollingInterval.Infinite, 
                retainedFileCountLimit: 1,
                fileSizeLimitBytes: 10_000_000, // 10MB
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }

    public static void SetupDebugLogger()
    {
        string logDirectory = "Logs";
        LogFilePath = Path.Combine(logDirectory, "Log.txt");
        
        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .WriteTo.File(LogFilePath, 
                rollingInterval: RollingInterval.Infinite, 
                retainedFileCountLimit: 1,
                fileSizeLimitBytes: 1_000_000,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }

    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }
}
