using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class RigidDiskPartitionInspectorTests
{
    [Fact]
    public void ReadsPartitionChain()
    {
        var image = new byte[64 * 512];
        var rdbBlock = image.AsSpan(2 * 512, 512);
        Write(rdbBlock, 0, 0x5244534B); Write(rdbBlock, 1, 64); Write(rdbBlock, 4, 512); Write(rdbBlock, 7, 3); Fix(rdbBlock, 64);

        var part = image.AsSpan(3 * 512, 512);
        Write(part, 0, 0x50415254); Write(part, 1, 64); Write(part, 4, 0xffffffff);
        Write(part, 5, 1); Write(part, 10, 128); Write(part, 11, 2); Write(part, 12, 11); Write(part, 20, 2); Write(part, 21, 79);\n        Write(part, 24, 0x001fe00); Write(part, 25, 0x7ffffffe); Write(part, 26, unchecked((uint)-5)); Write(part, 27, 0x444f5301);
        Write(part, 32, 3); Encoding.ASCII.GetBytes("DH0").CopyTo(part[132..]); Fix(part, 64);

        var rdb = RigidDiskBlockInspector.Inspect(image)!;
        var partitions = RigidDiskPartitionInspector.Inspect(image, rdb);

        var p = Assert.Single(partitions);
        Assert.Equal("DH0", p.Name);
        Assert.Equal((uint)2, p.LowCyl);
        Assert.Equal((uint)79, p.HighCyl);
        Assert.True(p.ChecksumValid);\n        Assert.True(p.Bootable);\n        Assert.False(p.NoMount);\n        Assert.Equal((uint)0x001fe00, p.MaxTransfer);\n        Assert.Equal((uint)0x7ffffffe, p.Mask);\n        Assert.Equal(-5, p.BootPriority);\n        Assert.Equal((uint)0x444f5301, p.DosType);
    }

    [Fact]
    public void RejectsPartitionListCycle()
    {
        var image = new byte[16 * 512];
        var rdb = image.AsSpan(0, 512);
        Write(rdb, 0, 0x5244534B); Write(rdb, 1, 64); Write(rdb, 4, 512); Write(rdb, 7, 1); Fix(rdb, 64);
        var part = image.AsSpan(512, 512);
        Write(part, 0, 0x50415254); Write(part, 1, 64); Write(part, 4, 1); Fix(part, 64);
        var info = RigidDiskBlockInspector.Inspect(image)!;
        Assert.Throws<InvalidDataException>(() => RigidDiskPartitionInspector.Inspect(image, info));
    }

    [Fact]
    public void RejectsPartitionOutsideImage()
    {
        var image = new byte[4 * 512];
        var rdb = image.AsSpan(0, 512);
        Write(rdb, 0, 0x5244534B); Write(rdb, 1, 64); Write(rdb, 4, 512); Write(rdb, 7, 100); Fix(rdb, 64);
        var info = RigidDiskBlockInspector.Inspect(image)!;
        Assert.Throws<InvalidDataException>(() => RigidDiskPartitionInspector.Inspect(image, info));
    }

    private static void Write(Span<byte> b, int i, uint v) => BinaryPrimitives.WriteUInt32BigEndian(b.Slice(i * 4, 4), v);
    private static void Fix(Span<byte> b, int longs)
    {
        Write(b, 2, 0); uint sum = 0;
        for (var i = 0; i < longs; i++) sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(b.Slice(i * 4, 4)));
        Write(b, 2, unchecked(0u - sum));
    }
}
