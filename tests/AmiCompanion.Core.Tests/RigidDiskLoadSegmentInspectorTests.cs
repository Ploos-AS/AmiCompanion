using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class RigidDiskLoadSegmentInspectorTests
{
    [Fact]
    public void ReadsLoadSegmentChain()
    {
        var image = new byte[16 * 512];
        MakeSegment(image.AsSpan(2 * 512, 512), 3, 8);
        MakeSegment(image.AsSpan(3 * 512, 512), 0xffffffff, 7);
        var rdb = new RigidDiskBlockInfo(0, 64, 0, 512, 0, 0, 0, 0, 0, 0, 0, 0, true);

        var segments = RigidDiskLoadSegmentInspector.Inspect(image, rdb, 2);

        Assert.Equal(2, segments.Count);
        Assert.Equal(12, segments[0].PayloadLength);
        Assert.Equal(8, segments[1].PayloadLength);
        Assert.All(segments, x => Assert.True(x.ChecksumValid));
    }

    [Fact]
    public void RejectsLoadSegmentCycle()
    {
        var image = new byte[8 * 512];
        MakeSegment(image.AsSpan(512, 512), 1, 6);
        var rdb = new RigidDiskBlockInfo(0, 64, 0, 512, 0, 0, 0, 0, 0, 0, 0, 0, true);
        Assert.Throws<InvalidDataException>(() => RigidDiskLoadSegmentInspector.Inspect(image, rdb, 1));
    }

    private static void MakeSegment(Span<byte> block, uint next, int longs)
    {
        Write(block, 0, 0x4c534547); Write(block, 1, (uint)longs); Write(block, 4, next);
        for (var i = 5; i < longs; i++) Write(block, i, (uint)i);
        Write(block, 2, 0);
        uint sum = 0;
        for (var i = 0; i < longs; i++) sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4, 4)));
        Write(block, 2, unchecked(0u - sum));
    }

    private static void Write(Span<byte> b, int i, uint v) =>
        BinaryPrimitives.WriteUInt32BigEndian(b.Slice(i * 4, 4), v);
}
