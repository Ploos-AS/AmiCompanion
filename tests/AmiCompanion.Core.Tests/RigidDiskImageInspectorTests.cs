using System.Buffers.Binary;
using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class RigidDiskImageInspectorTests
{
    [Fact]
    public void AggregatesRdbPartitionsAndFileSystems()
    {
        var image = new byte[16 * 512];
        var rdb = image.AsSpan(0, 512);
        Write(rdb, 0, 0x5244534b); Write(rdb, 1, 64); Write(rdb, 4, 512);
        Write(rdb, 7, 2); Write(rdb, 8, 3); Fix(rdb, 64);

        var part = image.AsSpan(2 * 512, 512);
        Write(part, 0, 0x50415254); Write(part, 1, 64); Write(part, 4, 0xffffffff);
        // Match the PART layout currently qualified by the dedicated inspector tests.
        part[36] = 3; "DH0"u8.CopyTo(part[37..]); Fix(part, 64);

        var fshd = image.AsSpan(3 * 512, 512);
        Write(fshd, 0, 0x46534844); Write(fshd, 1, 64); Write(fshd, 4, 0xffffffff);
        Write(fshd, 8, 0x444f5301); Write(fshd, 18, 4); Fix(fshd, 64);

        var lseg = image.AsSpan(4 * 512, 512);
        Write(lseg, 0, 0x4c534547); Write(lseg, 1, 6); Write(lseg, 4, 0xffffffff);
        Write(lseg, 5, 0x12345678); Fix(lseg, 6);

        var info = RigidDiskImageInspector.Inspect(image);

        Assert.Equal(image.Length, info.ImageSize);
        Assert.Single(info.Partitions);
        Assert.Equal("DH0", info.Partitions[0].Name);
        var fs = Assert.Single(info.FileSystems);
        Assert.Equal("FFS", fs.FileSystemName);
        Assert.Single(fs.Segments);
        Assert.Equal(4, fs.PayloadLength);
        Assert.True(fs.ChecksumValid);
    }

    [Fact]
    public void RejectsImageWithoutRdb() =>
        Assert.Throws<InvalidDataException>(() => RigidDiskImageInspector.Inspect(new byte[4096]));

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
