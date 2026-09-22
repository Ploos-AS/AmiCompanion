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
            File.WriteAllBytes(destination, ReadFile(image, volume.FileSystem, entry.HeaderBlock));
        }
    }

    private static byte[] ReadFile(ReadOnlySpan<byte> image, AmigaDosFileSystem fileSystem, int headerBlock) =>
        AmigaDosDataReader.ReadFile(image, fileSystem, headerBlock);

}
