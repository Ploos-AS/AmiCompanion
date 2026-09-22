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
        var size = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header.Slice(3 * 4, 4)));
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
        var countBlocks = checked((int)ReadU32(header, 2));
        var slots = Math.Min(countBlocks, 72);
        var offset = 0;
        var visited = new HashSet<int>();

        for (var seq = 1; seq <= slots && offset < size; seq++)
        {
            var block = ReadU32(header, 6 + (72 - seq));
            var number = checked((int)block);
            ValidateBlock(image, number, visited);
            var sector = image.Slice(number * BlockSize, BlockSize);
            var count = Math.Min(BlockSize, size - offset);
            sector[..count].CopyTo(result.AsSpan(offset));
            offset += count;
        }

        if (offset != size) throw new InvalidDataException("FFS data block table ended before the declared file size.");
        return result;
    }

    private static byte[] ReadOfs(ReadOnlySpan<byte> image, ReadOnlySpan<byte> header, int size)
    {
        var result = new byte[size];
        var block = ReadU32(header, 4);
        var offset = 0;
        var visited = new HashSet<int>();
        var sequence = 1;

        while (block != 0 && offset < size)
        {
            var number = checked((int)block);
            ValidateBlock(image, number, visited);
            var sector = image.Slice(number * BlockSize, BlockSize);
            if (ReadU32(sector, 0) != 8)
                throw new InvalidDataException("Invalid OFS data block.");
            var payload = checked((int)ReadU32(sector, 3));
            if (payload < 0 || payload > DataBytesPerBlock || payload > size - offset || ReadU32(sector, 2) != (uint)sequence)
                throw new InvalidDataException("Invalid OFS data block metadata.");
            sector.Slice(DataOffset, payload).CopyTo(result.AsSpan(offset));
            offset += payload;
            block = ReadU32(sector, 4);
            sequence++;
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
