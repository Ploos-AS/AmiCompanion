using System.Buffers.Binary;

namespace AmiCompanion.Core.DiskImages;

public static class RigidDiskLoadSegmentInspector
{
    private const uint End = 0xffffffff;

    public static IReadOnlyList<RigidDiskLoadSegmentInfo> Inspect(ReadOnlySpan<byte> image, RigidDiskBlockInfo rdb, uint firstBlock)
    {
        var result = new List<RigidDiskLoadSegmentInfo>();
        var next = firstBlock;
        var seen = new HashSet<uint>();

        while (next != End)
        {
            if (!seen.Add(next)) throw new InvalidDataException("RDB load segment list contains a cycle.");
            var offset64 = (ulong)next * rdb.BlockSize;
            if (offset64 > int.MaxValue || offset64 + 20 > (ulong)image.Length)
                throw new InvalidDataException("RDB load segment block is outside the image.");
            var offset = (int)offset64;
            var block = image[offset..];
            if (!block[..4].SequenceEqual("LSEG"u8))
                throw new InvalidDataException("Invalid RDB load segment block ID.");

            var longs = Read(block, 1);
            if (longs < 5 || (ulong)longs * 4 > (ulong)block.Length)
                throw new InvalidDataException("Invalid RDB load segment size.");

            uint sum = 0;
            for (var i = 0u; i < longs; i++) sum = unchecked(sum + Read(block, (int)i));

            result.Add(new RigidDiskLoadSegmentInfo(
                offset, longs, Read(block, 3), Read(block, 4), checked((int)((longs - 5) * 4)), sum == 0));

            next = Read(block, 4);
        }

        return result;
    }

    private static uint Read(ReadOnlySpan<byte> data, int index) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(index * 4, 4));
}
