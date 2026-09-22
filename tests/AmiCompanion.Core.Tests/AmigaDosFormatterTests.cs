using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosFormatterTests
{
    [Theory]
    [InlineData(AmigaDosFileSystem.Ofs, 0)]
    [InlineData(AmigaDosFileSystem.Ffs, 1)]
    public void FormatsStandardAdfWithRootAndBitmap(AmigaDosFileSystem fs, byte dosType)
    {
        var image = AmigaDosFormatter.FormatAdf("AmiCompanion", fs);
        Assert.Equal(dosType, image[3]);

        const int rootBlock = 880;
        const int bitmapBlock = 881;
        var root = image.AsSpan(rootBlock * 512, 512);
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(root[..4]));
        Assert.Equal(0xffffffffu, BinaryPrimitives.ReadUInt32BigEndian(root.Slice(78 * 4, 4)));
        Assert.Equal((uint)bitmapBlock, BinaryPrimitives.ReadUInt32BigEndian(root.Slice(79 * 4, 4)));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32BigEndian(root.Slice(127 * 4, 4)));
        Assert.Equal("AmiCompanion", System.Text.Encoding.Latin1.GetString(root.Slice(433, root[432])));
        Assert.Equal(0u, Sum(root));

        var bitmap = image.AsSpan(bitmapBlock * 512, 512);
        Assert.Equal(0u, Sum(bitmap));
        Assert.True(AmigaBootBlockInspector.Inspect(image).IsChecksumValid);
    }

    [Fact]
    public void RejectsTooLongVolumeName() =>
        Assert.Throws<ArgumentException>(() => AmigaDosFormatter.FormatAdf(new string('A', 31)));

    private static uint Sum(ReadOnlySpan<byte> block)
    {
        uint sum = 0;
        for (var i = 0; i < 128; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4)));
        return sum;
    }
}
