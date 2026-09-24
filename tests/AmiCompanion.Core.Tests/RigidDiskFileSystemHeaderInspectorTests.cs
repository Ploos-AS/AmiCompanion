using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class RigidDiskFileSystemHeaderInspectorTests
{
    [Fact]
    public void ReadsFileSystemHeader()
    {
        var image = new byte[32 * 512];
        var rdbBlock = image.AsSpan(0, 512);
        Write(rdbBlock, 0, 0x5244534B); Write(rdbBlock, 1, 64); Write(rdbBlock, 4, 512);
        Write(rdbBlock, 8, 2); Fix(rdbBlock, 64);

        var fshd = image.AsSpan(2 * 512, 512);
        Write(fshd, 0, 0x46534844); Write(fshd, 1, 64); Write(fshd, 4, 0xffffffff);
        Write(fshd, 8, 0x444f5303); Write(fshd, 9, 0x00280001);
        Write(fshd, 10, 0x180); Write(fshd, 18, 4); Fix(fshd, 64);

        var rdb = RigidDiskBlockInspector.Inspect(image)!;
        var fs = Assert.Single(RigidDiskFileSystemHeaderInspector.Inspect(image, rdb));

        Assert.Equal((uint)0x444f5303, fs.DosType);
        Assert.Equal((uint)0x00280001, fs.Version);
        Assert.Equal((uint)0x180, fs.PatchFlags);
        Assert.Equal((uint)4, fs.SegListBlocks);
        Assert.True(fs.ChecksumValid);
    }

    [Fact]
    public void RejectsFileSystemHeaderCycle()
    {
        var image = new byte[8 * 512];
        var rdbBlock = image.AsSpan(0, 512);
        Write(rdbBlock, 0, 0x5244534B); Write(rdbBlock, 1, 64); Write(rdbBlock, 4, 512);
        Write(rdbBlock, 8, 1); Fix(rdbBlock, 64);
        var fshd = image.AsSpan(512, 512);
        Write(fshd, 0, 0x46534844); Write(fshd, 1, 64); Write(fshd, 4, 1); Fix(fshd, 64);

        var rdb = RigidDiskBlockInspector.Inspect(image)!;
        Assert.Throws<InvalidDataException>(() => RigidDiskFileSystemHeaderInspector.Inspect(image, rdb));
    }

    private static void Write(Span<byte> b, int i, uint v) =>
        BinaryPrimitives.WriteUInt32BigEndian(b.Slice(i * 4, 4), v);

    private static void Fix(Span<byte> b, int longs)
    {
        Write(b, 2, 0);
        uint sum = 0;
        for (var i = 0; i < longs; i++)
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i * 4, 4)));
        Write(b, 2, unchecked(0u - sum));
    }
}
