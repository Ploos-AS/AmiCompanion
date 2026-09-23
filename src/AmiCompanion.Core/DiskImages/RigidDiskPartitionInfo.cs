namespace AmiCompanion.Core.DiskImages;

public sealed record RigidDiskPartitionInfo(
    int Offset,
    uint SummedLongs,
    uint HostId,
    uint Next,
    uint Flags,
    uint DevFlags,
    uint SizeBlock,
    uint SizeHeads,
    uint SizeSectors,
    uint LowCyl,
    uint HighCyl,
    string Name,
    bool ChecksumValid);
