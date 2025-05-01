using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Serilog;

namespace WpfApp1.Services;

public static class DialogService
{   
    public static bool GetFilePathsExplorerWindow(out string[] paths)
    {
        paths = [];

        OpenFileDialog openFileDialog = new()
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title = "Select one or more PDF files",
            Multiselect = true,
        };

        if (openFileDialog.ShowDialog() != true)
            return false;
        
        if (!PdfService.IsAllValidPdfExtension(openFileDialog.FileNames))
        {
            MessageBox.Show("Only PDF files are allowed!", "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        paths = openFileDialog.FileNames;
        return true;
    }

    public static bool GetFilePathsDragAndDrop(DragEventArgs e, out string[] paths)
    {
        paths = [];

        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) 
            return false;

        paths = (string[])e.Data.GetData(DataFormats.FileDrop);

        if (!PdfService.IsAllValidPdfExtension(paths))
        {
            MessageBox.Show("Only PDF files are allowed!", "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    public static void ShowCorruptedFilesDialog(List<string> corruptedFiles)
    {   
        StringBuilder sb = new();
        sb.Append("The following files are corrupted or invalid:\n\n");

        foreach (var file in corruptedFiles)
        {
            sb.Append($"{file}\n");
        }

        string message = sb.ToString();
        MessageBox.Show(
            message, 
            "Invalid or Corrupted Files",
            MessageBoxButton.OK, 
            MessageBoxImage.Warning
        );
    }
    
    public static bool PromptAddExistingFile(string path)
    {   
        var answer = MessageBox.Show(
            $"The file '{Path.GetFileName(path)}' already exists. \nDo you want to add it again?",
            "File Already Exists", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Warning
        );

        return answer == MessageBoxResult.Yes;
    }

    public static bool PromptLicenseAgreement()
    {   
        if (!Properties.Settings.Default.IsFirstStartup) return true;

        var answer = MessageBox.Show(
            "By using this software, you agree to do so entirely at your own risk.\n\n" +
            "The software is provided 'as is', without any warranties, under the terms of the MIT License.\n\n\n" +
            "By clicking OK, you acknowledge and accept these terms.\n\n",
            "Terms of Use",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning
        );

        if (answer == MessageBoxResult.OK)
        {
            Properties.Settings.Default.IsFirstStartup = false;
            Properties.Settings.Default.Save();
            Log.Warning("License agreement accepted.");
            return true;
        }

        return false;
    }

    public static void Error(string message)
    {
        MessageBox.Show(
            message,
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }

    public static void ErrorProcessingPDF(Exception ex)
    {
        MessageBox.Show(
            $"Error processing PDF: {ex.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }

    public static void ErrorGettingDesktop(Exception ex) // Can this ever happen?
    {
        MessageBox.Show(
            $"Error getting desktop: {ex.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        Log.Fatal($"Error getting desktop: {ex.Message}");
    }

    public static bool CheckAndConfirmFileOverwrite(string outputPdf)
    {
        if (File.Exists(outputPdf))
        {
            string fileName = Path.GetFileName(outputPdf);

            var answer = MessageBox.Show(
                $"The file \"{fileName}\" already exists. Do you want to overwrite it?",
                "File Exists",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (answer == MessageBoxResult.No)
            {
                return false;
            }
        }

        return true;
    }
}
