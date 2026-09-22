namespace AmiCompanion.Core.DiskImages;

public sealed record AdfInfo(
    long Size,
    int Cylinders,
    int Heads,
    int SectorsPerTrack,
    int BytesPerSector,
    string DosType,
    bool HasRecognizedDosType,
    bool HasStandardGeometry);
