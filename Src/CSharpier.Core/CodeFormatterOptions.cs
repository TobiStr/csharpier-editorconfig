namespace CSharpier.Core;

public class CodeFormatterOptions
{
    public int Width { get; init; } = 100;
    public IndentStyle IndentStyle { get; init; } = IndentStyle.Spaces;
    public int IndentSize { get; init; } = 4;
    public EndOfLine EndOfLine { get; init; } = EndOfLine.Auto;
    public bool IncludeGenerated { get; init; }
    public XmlWhitespaceSensitivity XmlWhitespaceSensitivity { get; init; } =
        XmlWhitespaceSensitivity.Strict;
    public BraceNewLine NewLineBeforeOpenBrace { get; init; } = BraceNewLine.All;
    public bool NewLineBeforeElse { get; init; } = true;
    public bool NewLineBeforeCatch { get; init; } = true;
    public bool NewLineBeforeFinally { get; init; } = true;
    public bool? NewLineBeforeMembersInObjectInitializers { get; init; }
    public bool? NewLineBeforeMembersInAnonymousTypes { get; init; }
    public bool? NewLineBetweenQueryExpressionClauses { get; init; }
    public bool UsePrettierStyleTrailingCommas { get; init; } = true;

    internal PrinterOptions ToPrinterOptions()
    {
        return new(Formatter.CSharp, this.XmlWhitespaceSensitivity)
        {
            Width = this.Width,
            UseTabs = this.IndentStyle == IndentStyle.Tabs,
            IndentSize = this.IndentSize,
            EndOfLine = this.EndOfLine,
            IncludeGenerated = this.IncludeGenerated,
            NewLineBeforeOpenBrace = this.NewLineBeforeOpenBrace,
            NewLineBeforeElse = this.NewLineBeforeElse,
            NewLineBeforeCatch = this.NewLineBeforeCatch,
            NewLineBeforeFinally = this.NewLineBeforeFinally,
            NewLineBeforeMembersInObjectInitializers =
                this.NewLineBeforeMembersInObjectInitializers,
            NewLineBeforeMembersInAnonymousTypes = this.NewLineBeforeMembersInAnonymousTypes,
            NewLineBetweenQueryExpressionClauses = this.NewLineBetweenQueryExpressionClauses,
            UsePrettierStyleTrailingCommas = this.UsePrettierStyleTrailingCommas,
        };
    }
}

public enum IndentStyle
{
    Spaces,
    Tabs,
}
