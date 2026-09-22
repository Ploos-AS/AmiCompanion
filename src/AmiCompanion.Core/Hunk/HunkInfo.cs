namespace AmiCompanion.Core.Hunk;

public sealed record HunkInfo(
    bool HasHeader,
    uint FirstHunk,
    uint LastHunk,
    IReadOnlyList<uint> HunkSizesLongwords);
