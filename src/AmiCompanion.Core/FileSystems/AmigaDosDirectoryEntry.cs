namespace AmiCompanion.Core.FileSystems;

public sealed record AmigaDosDirectoryEntry(
    string Name,
    int HeaderBlock,
    int SecondaryType,
    uint ByteSize,
    int NextHashBlock)
{
    public bool IsDirectory => SecondaryType == 2;
    public bool IsFile => SecondaryType == -3;
}
