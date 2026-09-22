using System.Buffers.Binary;

namespace AmiCompanion.Core.FileSystems;

internal static class AmigaDosDataReader
{
    private const int BlockSize = 512;
    private const int DataOffset = 24;
    private const int DataBytesPerBlock = 488;

    public static byte[] ReadFile(ReadOnlySpan<byte> image, AmigaDosFileSystem fileSystem, int headerBlock)
    {
        var header = image.Slice(headerBlock * BlockSize, BlockSize);
        var size = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header.Slice(81 * 4, 4)));
        if (size == 0) return Array.Empty<byte>();

        return fileSystem switch
        {
            AmigaDosFileSystem.Ofs => ReadOfs(image, header, size),
            AmigaDosFileSystem.Ffs => ReadFfs(image, header, size),
            _ => throw new InvalidDataException("Unsupported AmigaDOS filesystem.")
        };
    }

    private static byte[] ReadFfs(ReadOnlySpan<byte> image, ReadOnlySpan<byte> header, int size)
    {
        var result = new byte[size];
        var block = ReadU32(header, 125);
        var offset = 0;
        var visited = new HashSet<int>();

        while (block != 0 && offset < size)
        {
            var number = checked((int)block);
            ValidateBlock(image, number, visited);
            var sector = image.Slice(number * BlockSize, BlockSize);
            var count = Math.Min(DataBytesPerBlock, size - offset);
            sector.Slice(DataOffset, count).CopyTo(result.AsSpan(offset));
            offset += count;
            block = ReadU32(sector, 124);
        }

        if (offset != size) throw new InvalidDataException("FFS data chain ended before the declared file size.");
        return result;
    }

    private static byte[] ReadOfs(ReadOnlySpan<byte> image, ReadOnlySpan<byte> header, int size)
    {
        var result = new byte[size];
        var block = ReadU32(header, 125);
        var offset = 0;
        var visited = new HashSet<int>();

        while (block != 0 && offset < size)
        {
            var number = checked((int)block);
            ValidateBlock(image, number, visited);
            var sector = image.Slice(number * BlockSize, BlockSize);
            var payload = checked((int)ReadU32(sector, 4));
            if (payload < 0 || payload > DataBytesPerBlock || payload > size - offset)
                throw new InvalidDataException("Invalid OFS data block payload size.");
            sector.Slice(DataOffset, payload).CopyTo(result.AsSpan(offset));
            offset += payload;
            block = ReadU32(sector, 124);
        }

        if (offset != size) throw new InvalidDataException("OFS data chain ended before the declared file size.");
        return result;
    }

    private static void ValidateBlock(ReadOnlySpan<byte> image, int block, HashSet<int> visited)
    {
        if (block <= 0 || block >= image.Length / BlockSize || !visited.Add(block))
            throw new InvalidDataException("Invalid or cyclic AmigaDOS data chain.");
    }

    private static uint ReadU32(ReadOnlySpan<byte> block, int longword) =>
        BinaryPrimitives.ReadUInt32BigEndian(block.Slice(longword * 4, 4));
}
