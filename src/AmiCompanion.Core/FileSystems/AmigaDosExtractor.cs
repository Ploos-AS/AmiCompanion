using System.Buffers.Binary;
using System.Text;

namespace AmiCompanion.Core.FileSystems;

public static class AmigaDosExtractor
{
    private const int BlockSize = 512;

    public static void Extract(ReadOnlySpan<byte> image, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var volume = AmigaDosReader.Inspect(image);
        Directory.CreateDirectory(outputDirectory);

        foreach (var item in AmigaDosReader.ListAll(image))
        {
            var entry = item.Entry;
            var safeParts = item.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (safeParts.Any(part => part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                throw new InvalidDataException($"Unsafe Amiga path: {item.Path}");

            var destination = Path.GetFullPath(Path.Combine(new[] { outputDirectory }.Concat(safeParts).ToArray()));
            var root = Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(root, StringComparison.Ordinal))
                throw new InvalidDataException($"Extraction path escapes output directory: {item.Path}");

            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            if (!entry.IsFile)
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, ReadFile(image, entry.HeaderBlock));
        }
    }

    private static byte[] ReadFile(ReadOnlySpan<byte> image, int headerBlock)
    {
        var header = image.Slice(headerBlock * BlockSize, BlockSize);
        var byteSize = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header.Slice(81 * 4, 4)));
        if (byteSize == 0) return Array.Empty<byte>();

        var data = new List<byte>(byteSize);
        var block = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(125 * 4, 4));
        var visited = new HashSet<int>();

        while (block != 0 && data.Count < byteSize)
        {
            var number = checked((int)block);
            if (number <= 0 || number >= image.Length / BlockSize || !visited.Add(number))
                throw new InvalidDataException("Invalid or cyclic AmigaDOS file data chain.");

            var sector = image.Slice(number * BlockSize, BlockSize);
            var next = BinaryPrimitives.ReadUInt32BigEndian(sector.Slice(124 * 4, 4));
            var dataSize = Math.Min(488, byteSize - data.Count);
            data.AddRange(sector.Slice(24, dataSize).ToArray());
            block = next;
        }

        if (data.Count != byteSize)
            throw new InvalidDataException("AmigaDOS file data chain ended before the declared file size.");

        return data.ToArray();
    }
}
