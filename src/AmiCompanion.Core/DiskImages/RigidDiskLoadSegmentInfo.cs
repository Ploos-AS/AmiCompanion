namespace AmiCompanion.Core.DiskImages;

public sealed record RigidDiskLoadSegmentInfo(
    int Offset,
    uint SummedLongs,
    uint HostId,
    uint Next,
    int PayloadLength,
    bool ChecksumValid);
