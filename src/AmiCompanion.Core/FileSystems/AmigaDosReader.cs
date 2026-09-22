using System.Buffers.Binary;
using System.Text;
using AmiCompanion.Core.DiskImages;

namespace AmiCompanion.Core.FileSystems;

public sealed record AmigaDosVolumeInfo(string Name, AmigaDosFileSystem FileSystem, int RootBlock, int BitmapBlock, bool RootChecksumValid, bool BitmapChecksumValid);

public static class AmigaDosReader
{
    private const int BlockSize = 512;

    public static AmigaDosVolumeInfo Inspect(ReadOnlySpan<byte> image)
    {
        var adf = AdfInspector.Inspect(image);
        if (!adf.HasStandardGeometry || !adf.HasRecognizedDosType)
            throw new InvalidDataException("Not a supported standard AmigaDOS ADF image.");

        var fs = image[3] switch
        {
            0 => AmigaDosFileSystem.Ofs,
            1 => AmigaDosFileSystem.Ffs,
            _ => throw new InvalidDataException("Only DOS\\0 OFS and DOS\\1 FFS are supported.")
        };
        var rootBlock = (image.Length / BlockSize) / 2;
        var root = image.Slice(rootBlock * BlockSize, BlockSize);
        if (ReadU32(root, 0) != 2 || ReadU32(root, 127) != 1)
            throw new InvalidDataException("Invalid AmigaDOS root block.");
        var bitmapBlock = checked((int)ReadU32(root, 79));
        if (bitmapBlock <= 0 || bitmapBlock >= image.Length / BlockSize)
            throw new InvalidDataException("Invalid AmigaDOS bitmap block pointer.");

        var nameLength = Math.Min(root[432], (byte)30);
        var name = Encoding.Latin1.GetString(root.Slice(433, nameLength));
        var bitmap = image.Slice(bitmapBlock * BlockSize, BlockSize);
        return new AmigaDosVolumeInfo(name, fs, rootBlock, bitmapBlock, Sum(root) == 0, Sum(bitmap) == 0);
    }

    public static IReadOnlyList<AmigaDosDirectoryEntry> ListRoot(ReadOnlySpan<byte> image)
    {
        var volume = Inspect(image);
        var root = image.Slice(volume.RootBlock * BlockSize, BlockSize);
        var entries = new List<AmigaDosDirectoryEntry>();
        var visited = new HashSet<int>();

        for (var slot = 6; slot < 78; slot++)
        {
            var blockNumber = checked((int)ReadU32(root, slot));
            while (blockNumber != 0)
            {
                if (blockNumber < 0 || blockNumber >= image.Length / BlockSize)
                    throw new InvalidDataException("Directory hash chain points outside the image.");
                if (!visited.Add(blockNumber))
                    throw new InvalidDataException("Directory hash chain contains a cycle.");

                var header = image.Slice(blockNumber * BlockSize, BlockSize);
                if (ReadU32(header, 0) != 2)
                    throw new InvalidDataException("Directory entry has an invalid header type.");

                var nameLength = Math.Min(header[432], (byte)30);
                var name = Encoding.Latin1.GetString(header.Slice(433, nameLength));
                var secondaryType = unchecked((int)ReadU32(header, 127));
                var byteSize = ReadU32(header, 81);
                var nextHash = checked((int)ReadU32(header, 124));
                entries.Add(new AmigaDosDirectoryEntry(name, blockNumber, secondaryType, byteSize, nextHash));
                blockNumber = nextHash;
            }
        }
        return entries;
    }

    private static uint ReadU32(ReadOnlySpan<byte> block, int longword) =>
        BinaryPrimitives.ReadUInt32BigEndian(block.Slice(longword * 4, 4));

    private static uint Sum(ReadOnlySpan<byte> block)
    {
        uint sum = 0;
        for (var i = 0; i < BlockSize / 4; i++) sum = unchecked(sum + ReadU32(block, i));
        return sum;
    }
}
