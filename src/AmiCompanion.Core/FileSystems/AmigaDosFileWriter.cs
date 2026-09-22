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
        var blocks = new List<int>();
        var blockCount = image.Length / BlockSize;
        var needed = Math.Max(1, (content.Length + DataBytesPerBlock - 1) / DataBytesPerBlock);
        for (var b = 2; b < blockCount && blocks.Count < needed + 1; b++)
            if (!used.Contains(b)) blocks.Add(b);
        if (blocks.Count != needed + 1) throw new IOException("Not enough free AmigaDOS blocks.");

        var headerBlock = blocks[0];
        var dataBlocks = blocks.Skip(1).ToArray();
        var root = image.AsSpan(volume.RootBlock * BlockSize, BlockSize);
        var slot = Enumerable.Range(6, 72).FirstOrDefault(i => ReadU32(root, i) == 0, -1);
        if (slot < 0) throw new IOException("Root directory hash table is full.");
        WriteU32(root, slot, (uint)headerBlock);

        var header = image.AsSpan(headerBlock * BlockSize, BlockSize);
        WriteU32(header, 0, 2);
        WriteU32(header, 81, (uint)content.Length);
        WriteU32(header, 125, dataBlocks.Length == 0 ? 0u : (uint)dataBlocks[0]);
        WriteU32(header, 127, unchecked((uint)-3));
        header[432] = (byte)nameBytes.Length; nameBytes.CopyTo(header[433..]);
        FixChecksum(header, 5);

        for (var i = 0; i < dataBlocks.Length; i++)
        {
            var block = image.AsSpan(dataBlocks[i] * BlockSize, BlockSize);
            WriteU32(block, 0, 8);
            WriteU32(block, 124, i + 1 < dataBlocks.Length ? (uint)dataBlocks[i + 1] : 0);
            var count = Math.Min(DataBytesPerBlock, content.Length - i * DataBytesPerBlock);
            if (fileSystem == AmigaDosFileSystem.Ofs) WriteU32(block, 1, (uint)count);
            content.Slice(i * DataBytesPerBlock, count).CopyTo(block[DataOffset..]);
            FixChecksum(block, 5);
        }
        FixChecksum(root, 5);
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
