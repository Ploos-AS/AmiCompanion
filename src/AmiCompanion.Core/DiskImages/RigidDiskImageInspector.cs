namespace AmiCompanion.Core.DiskImages;

public static class RigidDiskImageInspector
{
    public static RigidDiskImageInfo Inspect(ReadOnlySpan<byte> image)
    {
        var rdb = RigidDiskBlockInspector.Inspect(image)
            ?? throw new InvalidDataException("No valid RDB header found.");
        var partitions = RigidDiskPartitionInspector.Inspect(image, rdb);
        var headers = RigidDiskFileSystemHeaderInspector.Inspect(image, rdb);
        var fileSystems = new List<RigidDiskFileSystemInfo>(headers.Count);
        foreach (var header in headers)
        {
            var segments = RigidDiskLoadSegmentInspector.Inspect(image, rdb, header.SegListBlocks);
            fileSystems.Add(new RigidDiskFileSystemInfo(header, segments));
        }

        return new RigidDiskImageInfo(image.Length, rdb, partitions, fileSystems);
    }
}
