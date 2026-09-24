namespace AmiCompanion.Core.DiskImages;

public sealed record RigidDiskFileSystemHeaderInfo(
    int Offset,
    uint SummedLongs,
    uint HostId,
    uint Next,
    uint Flags,
    uint DosType,
    uint Version,
    uint PatchFlags,
    uint SegListBlocks,
    bool ChecksumValid);
