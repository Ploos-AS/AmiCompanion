namespace AmiCompanion.Core.DiskImages;

public sealed record RigidDiskFileSystemInfo(
    RigidDiskFileSystemHeaderInfo Header,
    IReadOnlyList<RigidDiskLoadSegmentInfo> Segments)
{
    public int PayloadLength => Segments.Sum(x => x.PayloadLength);
    public bool ChecksumValid => Header.ChecksumValid && Segments.All(x => x.ChecksumValid);
    public string FileSystemName => AmigaDosType.Describe(Header.DosType);
}

public sealed record RigidDiskImageInfo(
    int ImageSize,
    RigidDiskBlockInfo Rdb,
    IReadOnlyList<RigidDiskPartitionInfo> Partitions,
    IReadOnlyList<RigidDiskFileSystemInfo> FileSystems);
