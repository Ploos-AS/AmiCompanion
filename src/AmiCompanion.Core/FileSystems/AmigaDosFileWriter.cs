using System.Buffers.Binary;
using System.Text;

namespace AmiCompanion.Core.FileSystems;

public static class AmigaDosFileWriter
{
    private const int BlockSize = 512;
    private const int DataOffset = 24;
    private const int DataBytesPerBlock = 488;

    public static void AddFile(byte[] image, AmigaDosFileSystem fileSystem, string name, ReadOnlySpan<byte> content)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var nameBytes = Encoding.Latin1.GetBytes(name);
        if (nameBytes.Length is 0 or > 30) throw new ArgumentException("AmigaDOS file name must be 1-30 bytes.", nameof(name));

        var volume = AmigaDosReader.Inspect(image);
        if (volume.FileSystem != fileSystem) throw new InvalidDataException("Filesystem type does not match the image.");
        if (AmigaDosReader.ListAll(image).Any(x => string.Equals(x.Path, name, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"File already exists: {name}");

        var used = new HashSet<int>(AmigaDosReader.ListAll(image).Select(x => x.Entry.HeaderBlock));
        used.Add(volume.RootBlock); used.Add(volume.BitmapBlock);
        foreach (var item in AmigaDosReader.ListAll(image))
            used.Add(item.Entry.HeaderBlock);
        var blocks = new List<int>();
        var blockCount = image.Length / BlockSize;
        var needed = content.Length == 0 ? 0 : (content.Length + (fileSystem == AmigaDosFileSystem.Ffs ? BlockSize : DataBytesPerBlock) - 1) / (fileSystem == AmigaDosFileSystem.Ffs ? BlockSize : DataBytesPerBlock);
        for (var b = 2; b < blockCount && blocks.Count < needed + 1; b++)
            if (!used.Contains(b)) blocks.Add(b);
        if (blocks.Count != needed + 1) throw new IOException("Not enough free AmigaDOS blocks.");

        var headerBlock = blocks[0];
        var dataBlocks = blocks.Skip(1).ToArray();
        var root = image.AsSpan(volume.RootBlock * BlockSize, BlockSize);
        var slot = 6 + AmigaDosHash.GetBucket(name);
        var existing = checked((int)ReadU32(root, slot));
        if (existing == 0)
            WriteU32(root, slot, (uint)headerBlock);
        else
        {
            var current = existing;
            while (true)
            {
                var currentBlock = image.AsSpan(current * BlockSize, BlockSize);
                var next = checked((int)ReadU32(currentBlock, 124));
                if (next == 0)
                {
                    WriteU32(currentBlock, 124, (uint)headerBlock);
                    FixChecksum(currentBlock, 5);
                    break;
                }
                current = next;
            }
        }
        MarkAllocated(image, volume.BitmapBlock, blocks);

        var header = image.AsSpan(headerBlock * BlockSize, BlockSize);
        WriteU32(header, 0, 2);
        WriteU32(header, 1, (uint)headerBlock);
        WriteU32(header, 2, (uint)dataBlocks.Length);
        WriteU32(header, 3, 0);
        WriteU32(header, 4, dataBlocks.Length == 0 ? 0u : (uint)dataBlocks[0]);
        WriteU32(header, 3, (uint)content.Length);
        WriteU32(header, 124, 0);
        WriteU32(header, 125, (uint)volume.RootBlock);
        WriteU32(header, 126, 0);
        WriteU32(header, 127, unchecked((uint)-3));
        for (var i = 0; i < dataBlocks.Length; i++)
            WriteU32(header, 6 + (72 - (i + 1)), (uint)dataBlocks[i]);
        header[432] = (byte)nameBytes.Length; nameBytes.CopyTo(header[433..]);
        FixChecksum(header, 5);

        for (var i = 0; i < dataBlocks.Length; i++)
        {
            var block = image.AsSpan(dataBlocks[i] * BlockSize, BlockSize);
            var bytesPerBlock = fileSystem == AmigaDosFileSystem.Ffs ? BlockSize : DataBytesPerBlock;
            var count = Math.Min(bytesPerBlock, content.Length - i * bytesPerBlock);
            if (fileSystem == AmigaDosFileSystem.Ofs)
            {
                WriteU32(block, 0, 8);
                WriteU32(block, 1, (uint)headerBlock);
                WriteU32(block, 2, (uint)(i + 1));
                WriteU32(block, 3, (uint)count);
                WriteU32(block, 4, i + 1 < dataBlocks.Length ? (uint)dataBlocks[i + 1] : 0);
                content.Slice(i * bytesPerBlock, count).CopyTo(block[DataOffset..]);
                FixChecksum(block, 5);
            }
            else
            {
                content.Slice(i * bytesPerBlock, count).CopyTo(block);
            }
        }
        FixChecksum(root, 5);
    }

    private static void MarkAllocated(byte[] image, int bitmapBlock, IEnumerable<int> blocks)
    {
        var bitmap = image.AsSpan(bitmapBlock * BlockSize, BlockSize);
        foreach (var blockNumber in blocks)
        {
            var bit = blockNumber - 2;
            var word = 1 + bit / 32;
            var bitInWord = bit % 32;
            var value = ReadU32(bitmap, word);
            WriteU32(bitmap, word, value & ~(1u << bitInWord));
        }
        FixChecksum(bitmap, 0);
    }

    private static uint ReadU32(ReadOnlySpan<byte> b, int n) => BinaryPrimitives.ReadUInt32BigEndian(b.Slice(n * 4, 4));
    private static void WriteU32(Span<byte> b, int n, uint v) => BinaryPrimitives.WriteUInt32BigEndian(b.Slice(n * 4, 4), v);
    private static void FixChecksum(Span<byte> b, int n)
    {
        WriteU32(b, n, 0); uint sum = 0;
        for (var i = 0; i < 128; i++) sum = unchecked(sum + ReadU32(b, i));
        WriteU32(b, n, unchecked(0u - sum));
    }
}
