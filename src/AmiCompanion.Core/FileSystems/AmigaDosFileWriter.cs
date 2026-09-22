using System.Buffers.Binary;
using System.Text;

namespace AmiCompanion.Core.FileSystems;

public static class AmigaDosFileWriter
{
    private const int BlockSize = 512;
    private const int DataOffset = 24;
    private const int DataBytesPerBlock = 488;

    public static void CreateDirectory(byte[] image, AmigaDosFileSystem fileSystem, string parentPath, string name)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var nameBytes = Encoding.Latin1.GetBytes(name);
        if (nameBytes.Length is 0 or > 30) throw new ArgumentException("AmigaDOS directory name must be 1-30 bytes.", nameof(name));

        var volume = AmigaDosReader.Inspect(image);
        if (volume.FileSystem != fileSystem) throw new InvalidDataException("Filesystem type does not match the image.");
        var normalizedParent = parentPath.Trim('/');
        var parentBlock = volume.RootBlock;
        if (normalizedParent.Length != 0)
        {
            var matches = AmigaDosReader.ListAll(image).Where(x => x.Entry.IsDirectory && string.Equals(x.Path, normalizedParent, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0) throw new DirectoryNotFoundException(parentPath);
            parentBlock = matches[0].Entry.HeaderBlock;
        }

        var fullPath = normalizedParent.Length == 0 ? name : normalizedParent + "/" + name;
        if (AmigaDosReader.ListAll(image).Any(x => string.Equals(x.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"Entry already exists: {fullPath}");

        var bitmap = image.AsSpan(volume.BitmapBlock * BlockSize, BlockSize);
        var blockCount = image.Length / BlockSize;
        var directoryBlock = -1;
        for (var b = 2; b < blockCount; b++)
            if (IsFree(bitmap, b)) { directoryBlock = b; break; }
        if (directoryBlock < 0) throw new IOException("Not enough free AmigaDOS blocks.");

        var parentHeader = image.AsSpan(parentBlock * BlockSize, BlockSize);
        var slot = 6 + AmigaDosHash.GetBucket(name);
        var existing = checked((int)ReadU32(parentHeader, slot));
        if (existing == 0) WriteU32(parentHeader, slot, (uint)directoryBlock);
        else
        {
            var current = existing;
            while (true)
            {
                var currentBlock = image.AsSpan(current * BlockSize, BlockSize);
                var next = checked((int)ReadU32(currentBlock, 124));
                if (next == 0) { WriteU32(currentBlock, 124, (uint)directoryBlock); FixChecksum(currentBlock, 5); break; }
                current = next;
            }
        }

        var directory = image.AsSpan(directoryBlock * BlockSize, BlockSize);
        directory.Clear();
        WriteU32(directory, 0, 2);
        WriteU32(directory, 1, (uint)directoryBlock);
        WriteU32(directory, 125, (uint)parentBlock);
        WriteU32(directory, 127, 2);
        directory[432] = (byte)nameBytes.Length;
        nameBytes.CopyTo(directory[433..]);
        FixChecksum(directory, 5);
        MarkAllocated(image, volume.BitmapBlock, new[] { directoryBlock });
        FixChecksum(parentHeader, 5);
    }

    public static void AddFile(byte[] image, AmigaDosFileSystem fileSystem, string name, ReadOnlySpan<byte> content) =>
        AddFile(image, fileSystem, string.Empty, name, content);

    public static void AddFile(byte[] image, AmigaDosFileSystem fileSystem, string directoryPath, string name, ReadOnlySpan<byte> content)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var nameBytes = Encoding.Latin1.GetBytes(name);
        if (nameBytes.Length is 0 or > 30) throw new ArgumentException("AmigaDOS file name must be 1-30 bytes.", nameof(name));

        var volume = AmigaDosReader.Inspect(image);
        if (volume.FileSystem != fileSystem) throw new InvalidDataException("Filesystem type does not match the image.");
        var parentBlock = volume.RootBlock;
        var normalizedDirectory = directoryPath.Trim('/');
        if (normalizedDirectory.Length != 0)
        {
            var matches = AmigaDosReader.ListAll(image).Where(x => x.Entry.IsDirectory && string.Equals(x.Path, normalizedDirectory, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0) throw new DirectoryNotFoundException(directoryPath);
            parentBlock = matches[0].Entry.HeaderBlock;
        }
        var fullPath = normalizedDirectory.Length == 0 ? name : normalizedDirectory + "/" + name;
        if (AmigaDosReader.ListAll(image).Any(x => string.Equals(x.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
            throw new IOException($"File already exists: {fullPath}");

        var blocks = new List<int>();
        var blockCount = image.Length / BlockSize;
        var needed = content.Length == 0 ? 0 : (content.Length + (fileSystem == AmigaDosFileSystem.Ffs ? BlockSize : DataBytesPerBlock) - 1) / (fileSystem == AmigaDosFileSystem.Ffs ? BlockSize : DataBytesPerBlock);
        var bitmap = image.AsSpan(volume.BitmapBlock * BlockSize, BlockSize);
        for (var b = 2; b < blockCount && blocks.Count < needed + 1; b++)
            if (IsFree(bitmap, b)) blocks.Add(b);
        if (blocks.Count != needed + 1) throw new IOException("Not enough free AmigaDOS blocks.");

        var headerBlock = blocks[0];
        var dataBlocks = blocks.Skip(1).ToArray();
        var parentHeader = image.AsSpan(parentBlock * BlockSize, BlockSize);
        var slot = 6 + AmigaDosHash.GetBucket(name);
        var existing = checked((int)ReadU32(parentHeader, slot));
        if (existing == 0)
            WriteU32(parentHeader, slot, (uint)headerBlock);
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
        WriteU32(header, 3, 0); WriteU32(header, 81, (uint)content.Length);
        WriteU32(header, 124, 0);
        WriteU32(header, 125, (uint)parentBlock);
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
        FixChecksum(parentHeader, 5);
    }

    private static bool IsFree(ReadOnlySpan<byte> bitmap, int blockNumber)
    {
        var bit = blockNumber - 2;
        var word = 1 + bit / 32;
        var bitInWord = bit % 32;
        return (ReadU32(bitmap, word) & (1u << bitInWord)) != 0;
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
