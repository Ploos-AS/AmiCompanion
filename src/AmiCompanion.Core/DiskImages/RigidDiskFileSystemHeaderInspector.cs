using System.Buffers.Binary;

namespace AmiCompanion.Core.DiskImages;

public static class RigidDiskFileSystemHeaderInspector
{
    private const uint End = 0xffffffff;

    public static IReadOnlyList<RigidDiskFileSystemHeaderInfo> Inspect(ReadOnlySpan<byte> image, RigidDiskBlockInfo rdb)
    {
        var result = new List<RigidDiskFileSystemHeaderInfo>();
        var next = rdb.FileSystemHeaderList;
        var seen = new HashSet<uint>();

        while (next != End)
        {
            if (!seen.Add(next)) throw new InvalidDataException("RDB filesystem header list contains a cycle.");
            var offset64 = (ulong)next * rdb.BlockSize;
            if (offset64 > int.MaxValue || offset64 + 80 > (ulong)image.Length)
                throw new InvalidDataException("RDB filesystem header block is outside the image.");
            var offset = (int)offset64;
            var block = image[offset..];
            if (!block[..4].SequenceEqual("FSHD"u8))
                throw new InvalidDataException("Invalid RDB filesystem header block ID.");

            var longs = Read(block, 1);
            if (longs < 20 || (ulong)longs * 4 > (ulong)block.Length)
                throw new InvalidDataException("Invalid RDB filesystem header size.");

            uint sum = 0;
            for (var i = 0u; i < longs; i++) sum = unchecked(sum + Read(block, (int)i));

            result.Add(new RigidDiskFileSystemHeaderInfo(
                offset, longs, Read(block, 3), Read(block, 4), Read(block, 5),
                Read(block, 8), Read(block, 9), Read(block, 10), Read(block, 18), sum == 0));

            next = Read(block, 4);
        }

        return result;
    }

    private static uint Read(ReadOnlySpan<byte> data, int index) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(index * 4, 4));
}
