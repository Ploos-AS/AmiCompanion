using System.Buffers.Binary;
using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosWriterAllocationTests
{
    [Fact]
    public void WriterClearsBitmapBitsForAllocatedBlocks()
    {
        var image = AmigaDosFormatter.FormatAdf("ALLOC", AmigaDosFileSystem.Ffs);
        var before = image.AsSpan(881 * 512, 512).ToArray();
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "hello", new byte[10]);

        var entries = AmigaDosReader.ListAll(image);
        var allocated = entries.Select(x => x.Entry.HeaderBlock).ToArray();
        var bitmap = image.AsSpan(881 * 512, 512);

        foreach (var block in allocated)
        {
            var bit = block - 2;
            var word = 1 + bit / 32;
            var mask = 1u << (bit % 32);
            var value = BinaryPrimitives.ReadUInt32BigEndian(bitmap.Slice(word * 4, 4));
            Assert.Equal(0u, value & mask);
        }

        Assert.NotEqual(before, bitmap.ToArray());
        Assert.Equal(0u, Sum(bitmap));
    }

    [Fact]
    public void HashCollisionFormsChain()
    {
        var image = AmigaDosFormatter.FormatAdf("COLLIDE", AmigaDosFileSystem.Ffs);
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "file_1a", new byte[] { 1 });
        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "file_24", new byte[] { 2 });

        var entries = AmigaDosReader.ListAll(image);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, x => x.Path == "file_1a");
        Assert.Contains(entries, x => x.Path == "file_24");
    }

    [Fact]
    public void WriterSkipsBlockMarkedAllocatedInBitmap()
    {
        var image = AmigaDosFormatter.FormatAdf("ALLOC", AmigaDosFileSystem.Ffs);
        var bitmap = image.AsSpan(881 * 512, 512);
        const int reservedBlock = 2;
        var bit = reservedBlock - 2;
        var word = 1 + bit / 32;
        var mask = 1u << (bit % 32);
        var value = BinaryPrimitives.ReadUInt32BigEndian(bitmap.Slice(word * 4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(bitmap.Slice(word * 4, 4), value & ~mask);
        FixBitmapChecksum(bitmap);

        AmigaDosFileWriter.AddFile(image, AmigaDosFileSystem.Ffs, "safe", new byte[10]);

        var entry = Assert.Single(AmigaDosReader.ListAll(image));
        Assert.NotEqual(reservedBlock, entry.Entry.HeaderBlock);
        Assert.NotEqual(reservedBlock, ReadDataBlock(image, entry.Entry.HeaderBlock));
    }

    private static int ReadDataBlock(byte[] image, int headerBlock)
    {
        var header = image.AsSpan(headerBlock * 512, 512);
        return checked((int)BinaryPrimitives.ReadUInt32BigEndian(header.Slice(77 * 4, 4)));
    }

    private static void FixBitmapChecksum(Span<byte> bitmap)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bitmap[..4], 0);
        var sum = Sum(bitmap);
        BinaryPrimitives.WriteUInt32BigEndian(bitmap[..4], unchecked(0u - sum));
    }

    private static uint Sum(ReadOnlySpan<byte> block)
    {
        uint sum = 0;
        for (var i = 0; i < 128; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4)));
        return sum;
    }
}
