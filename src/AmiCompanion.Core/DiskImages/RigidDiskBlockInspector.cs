using System.Buffers.Binary;

namespace AmiCompanion.Core.DiskImages;

public static class RigidDiskBlockInspector
{
    private const int ScanBlocks = 16;
    private const int MinimumLongs = 64;

    public static RigidDiskBlockInfo? Inspect(ReadOnlySpan<byte> image)
    {
        for (var block = 0; block < ScanBlocks; block++)
        {
            var offset = block * 512;
            if (offset + 256 > image.Length) break;
            var data = image[offset..];
            if (!data[..4].SequenceEqual("RDSK"u8)) continue;

            var summedLongs = Read(data, 1);
            if (summedLongs < MinimumLongs || summedLongs > 128 || summedLongs * 4 > data.Length)
                continue;

            uint sum = 0;
            for (var i = 0; i < summedLongs; i++)
                sum = unchecked(sum + Read(data, i));

            var blockSize = Read(data, 4);
            if (blockSize < 512 || blockSize > 65536 || (blockSize & 3) != 0)
                continue;

            return new RigidDiskBlockInfo(
                offset, summedLongs, Read(data, 3), blockSize, Read(data, 5),
                Read(data, 6), Read(data, 7), Read(data, 8), Read(data, 9),
                Read(data, 16), Read(data, 17), Read(data, 18), sum == 0);
        }

        return null;
    }

    private static uint Read(ReadOnlySpan<byte> data, int index) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(index * 4, 4));
}
