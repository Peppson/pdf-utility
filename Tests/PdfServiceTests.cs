using System.Reflection;
using WpfApp1.Services;

namespace Tests;

public class PdfServiceTests
{   
    [Theory]
    [InlineData(new string[] { "file3.pdf" }, true)]
    [InlineData(new string[] { "file1.pdf", "file2.pdf", "file3.pdf" }, true)]
    [InlineData(new string[] { "file1.pdf", "file2.txt", "file3.pdf" }, false)]
    [InlineData(new string[] { "file1.txt", "file2.txt" }, false)]
    [InlineData(new string[] { "file1", "file2" }, false)]
    [InlineData(new string[] { }, false)]
    public void IsAllFilesPdf_ReturnsExpectedResult_ForVariousFiles(string[] files, bool expected)
    {
        var actual = PdfService.IsAllValidPdfExtension(files);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckAndConfirmFileOverwrite_FileDoesNotExist_ReturnsTrue()
    {
        var instance = new PdfService(new WinDialogService());

        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");

        MethodInfo methodInfo = typeof(PdfService).GetMethod("CheckAndConfirmFileOverwrite", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        bool result = (bool)methodInfo.Invoke(instance, [nonExistentPath])!;

        Assert.True(result);
    }
}


