using System.Buffers.Binary;
using System.Text;

namespace AmiCompanion.Core.DiskImages;

public static class RigidDiskPartitionInspector
{
    public static IReadOnlyList<RigidDiskPartitionInfo> Inspect(ReadOnlySpan<byte> image, RigidDiskBlockInfo rdb)
    {
        var result = new List<RigidDiskPartitionInfo>();
        var next = rdb.PartitionList;
        var seen = new HashSet<uint>();

        while (next != 0xffffffff)
        {
            if (!seen.Add(next)) throw new InvalidDataException("RDB partition list contains a cycle.");
            var offset = checked((int)(next * rdb.BlockSize));
            if (offset < 0 || offset + 128 > image.Length) throw new InvalidDataException("RDB partition block is outside the image.");
            var block = image[offset..];
            if (!block[..4].SequenceEqual("PART"u8)) throw new InvalidDataException("Invalid RDB partition block ID.");
            var longs = Read(block, 1);
            if (longs < 64 || longs * 4 > block.Length) throw new InvalidDataException("Invalid RDB partition block size.");

            uint sum = 0;
            for (var i = 0; i < longs; i++) sum = unchecked(sum + Read(block, i));

            var nameLength = Math.Min(Read(block, 32), 31u);
            var nameBytes = block.Slice(33 * 4, (int)nameLength);
            var name = Encoding.ASCII.GetString(nameBytes);

            result.Add(new RigidDiskPartitionInfo(
                offset, longs, Read(block, 3), Read(block, 4), Read(block, 5),
                Read(block, 6), Read(block, 10), Read(block, 11), Read(block, 12),
                Read(block, 20), Read(block, 21), name, sum == 0));

            next = Read(block, 4);
        }

        return result;
    }

    private static uint Read(ReadOnlySpan<byte> data, int index) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(index * 4, 4));
}
