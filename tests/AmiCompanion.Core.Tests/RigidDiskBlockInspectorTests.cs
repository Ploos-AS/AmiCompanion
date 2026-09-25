using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class RigidDiskBlockInspectorTests
{
    [Fact]
    public void InspectsValidRdbAndChecksum()
    {
        var image = new byte[32 * 512];
        var block = image.AsSpan(2 * 512, 512);
        Write(block, 0, 0x5244534B);
        Write(block, 1, 64);
        Write(block, 3, 7);
        Write(block, 4, 512);
        Write(block, 16, 100);
        Write(block, 17, 11);
        Write(block, 18, 2);
        Write(block, 6, 0xffffffff);
        Write(block, 7, 3);
        Write(block, 8, 4);
        FixChecksum(block, 64);

        var rdb = RigidDiskBlockInspector.Inspect(image);

        Assert.NotNull(rdb);
        Assert.Equal(2 * 512, rdb!.Offset);
        Assert.Equal((uint)64, rdb.SummedLongs);
        Assert.Equal((uint)512, rdb.BlockSize);
        Assert.Equal((uint)100, rdb.Cylinders);
        Assert.Equal((uint)11, rdb.Sectors);
        Assert.Equal((uint)2, rdb.Heads);
        Assert.True(rdb.ChecksumValid);
    }

    [Fact]
    public void RejectsInvalidChecksum()
    {
        var image = new byte[4 * 512];
        var block = image.AsSpan(0, 512);
        Write(block, 0, 0x5244534B);
        Write(block, 1, 64);
        Write(block, 4, 512);
        FixChecksum(block, 64);
        block[100] ^= 1;

        var rdb = RigidDiskBlockInspector.Inspect(image);

        Assert.NotNull(rdb);
        Assert.False(rdb!.ChecksumValid);
    }

    [Fact]
    public void RejectsInvalidSummedLongs()
    {
        var image = new byte[4 * 512];
        var block = image.AsSpan(0, 512);
        Write(block, 0, 0x5244534B);
        Write(block, 1, 63);
        Write(block, 4, 512);

        Assert.Null(RigidDiskBlockInspector.Inspect(image));
    }

    [Fact]
    public void SkipsMalformedCandidateAndFindsLaterRdb()
    {
        var image = new byte[16 * 512];
        var malformed = image.AsSpan(0, 512);
        Write(malformed, 0, 0x5244534B);
        Write(malformed, 1, 63);

        var valid = image.AsSpan(2 * 512, 512);
        Write(valid, 0, 0x5244534B);
        Write(valid, 1, 64);
        Write(valid, 4, 512);
        FixChecksum(valid, 64);

        Assert.Equal(2 * 512, RigidDiskBlockInspector.Inspect(image)!.Offset);
    }

    [Fact]
    public void FindsRdbWithinInitialScanArea()
    {
        var image = new byte[16 * 512];
        var block = image.AsSpan(1 * 512, 512);
        Write(block, 0, 0x5244534B);
        Write(block, 1, 64);
        Write(block, 4, 512);
        FixChecksum(block, 64);

        Assert.Equal(512, RigidDiskBlockInspector.Inspect(image)!.Offset);
    }

    private static void Write(Span<byte> block, int index, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(block.Slice(index * 4, 4), value);

    private static void FixChecksum(Span<byte> block, int longs)
    {
        Write(block, 2, 0);
        uint sum = 0;
        for (var i = 0; i < longs; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4)));
        Write(block, 2, unchecked(0u - sum));
    }
}
