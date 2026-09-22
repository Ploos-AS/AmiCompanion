using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosMultiBlockTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void RoundTripsFileLargerThanOneDataBlock(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("MULTI", fs);
        var payload = Enumerable.Range(0, fs == AmigaDosFileSystem.Ofs ? 1200 : 1300)
            .Select(i => (byte)(i & 0xff)).ToArray();

        AmigaDosFileWriter.AddFile(image, fs, "large.bin", payload);

        var output = Path.Combine(Path.GetTempPath(), "amic-multi-" + Guid.NewGuid().ToString("N"));
        try
        {
            AmigaDosExtractor.Extract(image, output);
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(output, "large.bin")));
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }

    [Fact]
    public void CanAddMultipleFiles()
    {
        var image = AmigaDosFormatter.FormatAdf("MULTI", AmigaDosFileSystem.Ffs);
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "one", new byte[] { 1, 2, 3 });
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "two", new byte[] { 4, 5, 6 });

        var entries = AmigaDosReader.ListAll(image);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, x => x.Path == "one");
        Assert.Contains(entries, x => x.Path == "two");
    }
}
