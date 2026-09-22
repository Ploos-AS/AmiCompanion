using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosDirectoryTests
{
    [Fact]
    public void ListsSyntheticRootFile()
    {
        var image = AmigaDosFormatter.FormatAdf("TEST");
        const int headerBlock = 100;
        var root = image.AsSpan(880 * 512, 512);
        WriteU32(root, 6, headerBlock);
        FixChecksum(root, 5);

        var header = image.AsSpan(headerBlock * 512, 512);
        WriteU32(header, 0, 2);
        WriteU32(header, 81, 1234);
        header[432] = 5;
        Encoding.Latin1.GetBytes("hello").CopyTo(header[433..]);
        WriteU32(header, 124, 0);
        WriteU32(header, 127, unchecked((uint)-3));

        var entries = AmigaDosReader.ListRoot(image);
        var entry = Assert.Single(entries);
        Assert.Equal("hello", entry.Name);
        Assert.Equal(headerBlock, entry.HeaderBlock);
        Assert.Equal(1234u, entry.ByteSize);
        Assert.True(entry.IsFile);
        Assert.False(entry.IsDirectory);
    }

    private static void WriteU32(Span<byte> block, int longword, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(block.Slice(longword * 4, 4), value);

    private static void FixChecksum(Span<byte> block, int checksumLongword)
    {
        WriteU32(block, checksumLongword, 0);
        uint sum = 0;
        for (var i = 0; i < 128; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4)));
        WriteU32(block, checksumLongword, unchecked(0u - sum));
    }
}
