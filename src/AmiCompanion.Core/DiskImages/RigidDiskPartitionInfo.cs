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
    bool ChecksumValid,
    uint MaxTransfer,
    uint Mask,
    int BootPriority,
    uint DosType)
{
    public bool Bootable => (Flags & 1) != 0;
    public bool NoMount => (Flags & 2) != 0;
}
