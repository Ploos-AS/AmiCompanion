using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosReaderTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs)]
    [InlineData(AmigaDosFileSystem.Ffs)]
    public void ReadsFormattedVolume(AmigaDosFileSystem fs)
    {
        var image = AmigaDosFormatter.FormatAdf("TESTDISK", fs);
        var info = AmigaDosReader.Inspect(image);
        Assert.Equal("TESTDISK", info.Name);
        Assert.Equal(fs, info.FileSystem);
        Assert.Equal(880, info.RootBlock);
        Assert.Equal(881, info.BitmapBlock);
        Assert.True(info.RootChecksumValid);
        Assert.True(info.BitmapChecksumValid);
    }

    [Fact]
    public void RejectsCorruptRootBlock()
    {
        var image = AmigaDosFormatter.FormatAdf("TEST");
        image[880 * 512 + 3] = 0;
        Assert.Throws<InvalidDataException>(() => AmigaDosReader.Inspect(image));
    }
}
