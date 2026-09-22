using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosWriterTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void AddsFileAndRoundTrips(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("WRITE", fs);
        var payload = System.Text.Encoding.UTF8.GetBytes("writer test");
        AmigaDosFileWriter.AddFile(image, fs, "hello.txt", payload);

        var entry = Assert.Single(AmigaDosReader.ListAll(image));
        Assert.Equal("hello.txt", entry.Path);

        var output = Path.Combine(Path.GetTempPath(), "amic-writer-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, "hello.txt")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    [Fact]
    public void RejectsDuplicateFile()
    {
        var image = AmigaDosFormatter.FormatAdf("WRITE");
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "hello", new byte[] { 1 });
        Assert.Throws<IOException>(() => AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "hello", new byte[] { 2 }));
    }
}
