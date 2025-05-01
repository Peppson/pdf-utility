using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.Views;

public partial class MainWindow : Window
{
    private readonly IPdfService _PdfService;


    public MainWindow(IPdfService pdfService)
    {
        InitializeComponent();
        _PdfService = pdfService;
    }


    private void DoTheThing_Click(object sender, RoutedEventArgs e)
    {
        _PdfService.DoTheThing();
        _PdfService.RemoveAllPdfs();
        e.Handled = true;
    }


    private void SelectPdf_Click(object sender, RoutedEventArgs e)
    {
        _PdfService.SelectPdf();
        FileCountTextBlock.Text = $"Files: {_PdfService.FileCount}"; // todo
        e.Handled = true;
    }

    private void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        _PdfService.ResetAll();
        e.Handled = true;
    }

    private void DragAndDropPdf_Enter(object sender, DragEventArgs e)
    {
        // Mouse hover with files?
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        if (PdfService.IsAllValidPdfExtension(files))
        {
            TEST1.Fill = new SolidColorBrush(Colors.Green);
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            TEST1.Fill = new SolidColorBrush(Colors.Red);
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void DragAndDropPdf_Leave(object sender, DragEventArgs e)
    {
        TEST1.Fill = new SolidColorBrush(Colors.FloralWhite);
        e.Handled = true;
    }

    private void DragAndDropPdf_Drop(object sender, DragEventArgs e)
    {
        TEST1.Fill = new SolidColorBrush(Colors.FloralWhite);
        _PdfService.DragAndDropPdf(e);
        FileCountTextBlock.Text = $"Files: {_PdfService.FileCount}"; // todo
        e.Handled = true;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        if (Properties.Settings.Default.DarkTheme)
        {
            TEST2.Background = new SolidColorBrush(Colors.FloralWhite);
            Properties.Settings.Default.DarkTheme = false;
        }
        else 
        {
            TEST2.Background = new SolidColorBrush(Colors.Brown);
            Properties.Settings.Default.DarkTheme = true;
        }

        Properties.Settings.Default.Save();    
        e.Handled = true;
    }

    private void RemoveAllPdf_Click(object sender, RoutedEventArgs e)
    {
        _PdfService.RemoveAllPdfs();
        FileCountTextBlock.Text = $"Files: {_PdfService.FileCount}"; // todo
        e.Handled = true;
    }

    private void OpenLogFile_Click(object sender, RoutedEventArgs e)
    {
        var path = LogService.LogFilePath;

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        else
            MessageBox.Show("Log file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
        e.Handled = true;
    }     
}
