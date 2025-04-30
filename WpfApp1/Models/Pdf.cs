namespace WpfApp1.Models; 

public class Pdf(byte[] rawBytes, string filePath)
{
    public byte[] RawBytes { get; } = rawBytes;
    public string FilePath { get; } = filePath;
    public string FileName => System.IO.Path.GetFileName(FilePath);
}
