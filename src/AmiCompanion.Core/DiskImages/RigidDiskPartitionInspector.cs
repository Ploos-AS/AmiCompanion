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

            const int driveNameOffset = 9 * 4;
            var nameLength = Math.Min(block[driveNameOffset], (byte)31);
            var name = Encoding.ASCII.GetString(block.Slice(driveNameOffset + 1, nameLength));

            const int environment = 32;
            result.Add(new RigidDiskPartitionInfo(
                offset, longs, Read(block, 3), Read(block, 4), Read(block, 5),
                Read(block, 8), Read(block, environment + 1), Read(block, environment + 3), Read(block, environment + 5),
                Read(block, environment + 9), Read(block, environment + 10), name, sum == 0,
                Read(block, environment + 13), Read(block, environment + 14),
                unchecked((int)Read(block, environment + 15)), Read(block, environment + 16)));

            next = Read(block, 4);
        }

        return result;
    }

    private static uint Read(ReadOnlySpan<byte> data, int index) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(index * 4, 4));
}
