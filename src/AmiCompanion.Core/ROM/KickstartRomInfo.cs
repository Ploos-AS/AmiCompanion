namespace AmiCompanion.Core.ROM;

public sealed record KickstartRomInfo(
    long Size,
    ushort Version,
    ushort Revision,
    bool HasExecSignature,
    uint Checksum,
    bool IsChecksumValid);
