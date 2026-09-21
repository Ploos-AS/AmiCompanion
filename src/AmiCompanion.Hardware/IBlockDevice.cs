namespace AmiCompanion.Hardware;

public interface IBlockDevice
{
    string Id { get; }
    string DisplayName { get; }
    long Size { get; }
    bool IsRemovable { get; }
}
