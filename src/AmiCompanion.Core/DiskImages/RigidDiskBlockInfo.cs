namespace AmiCompanion.Core.DiskImages;

public sealed record RigidDiskBlockInfo(
    int Offset,
    uint SummedLongs,
    uint HostId,
    uint BlockSize,
    uint Flags,
    uint BadBlockList,
    uint PartitionList,
    uint FileSystemHeaderList,
    uint DriveInit,
    uint Cylinders,
    uint Sectors,
    uint Heads,
    bool ChecksumValid);
