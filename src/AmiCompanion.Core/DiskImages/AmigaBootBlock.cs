using System.Buffers.Binary;

namespace AmiCompanion.Core.DiskImages;

public sealed record AmigaBootBlock(
    string DosType,
    uint StoredChecksum,
    uint CalculatedChecksum,
    uint RootBlock,
    bool HasRecognizedDosType,
    bool IsChecksumValid);
