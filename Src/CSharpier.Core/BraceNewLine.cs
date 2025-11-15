namespace CSharpier.Core;

// Subset compatible with the original CSharpier BraceNewLine flags.
[System.Flags]
public enum BraceNewLine
{
    None = 0,
    Types = 1 << 0,
    Methods = 1 << 1,
    ControlBlocks = 1 << 2,

    All = 0xFFFF,
}
