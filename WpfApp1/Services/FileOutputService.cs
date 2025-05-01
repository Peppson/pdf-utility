using System.IO;
using Serilog;
using WpfApp1.Config;

namespace WpfApp1.Services;

public interface IFileOutputService
{
    string GetOutputPath();
    void OutputIndexFile(string content, string outputPath);
}

public class FileOutputService
{
    public static string GetOutputPath()
    {
        string desktopPath = GetDesktopFolder();
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

    public static void OutputIndexFile(string content, string outputPath)
    {   
        if (string.IsNullOrEmpty(content)) return;

        string filePath = outputPath + "Index.txt";
        if (!File.Exists(filePath)) 
            File.Delete(filePath);

        using var indexFile = new StreamWriter(filePath, append: true);
        indexFile.WriteLine("---- Index of processed files ----\n");
        indexFile.WriteLine(content);
    }

    private static string GetDesktopFolder()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (string.IsNullOrEmpty(desktopPath))
            DialogService.ErrorGettingDesktop(new Exception("Cannot find desktop."));

        // Create folder
        var outputFolder = Path.Combine(desktopPath, AppConstants.outputFolderName);
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            Log.Debug("Creating folder: " + outputFolder);
        }

        return outputFolder;
    }
}
