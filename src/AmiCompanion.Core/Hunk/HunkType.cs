namespace AmiCompanion.Core.Hunk;

public enum HunkType : uint
{
    Unit = 999,
    Name = 1000,
    Code = 1001,
    Data = 1002,
    Bss = 1003,
    Reloc32 = 1004,
    Reloc16 = 1005,
    Reloc8 = 1006,
    Ext = 1007,
    Symbol = 1008,
    Debug = 1009,
    End = 1010,
    Header = 1011,
    Overlay = 1013,
    Break = 1014,
    Reloc32Short = 1020,
    Reloc32Rel = 1021
}
