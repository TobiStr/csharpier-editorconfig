using CSharpier.Core.DocTypes;
using Microsoft.CodeAnalysis;

namespace CSharpier.Core.CSharp.SyntaxPrinter;

internal class CSharpPrintingContext : BasePrintingContext
{
    public PrintingContextState State { get; } = new();

    public CSharpPrintingContext WithSkipNextLeadingTrivia()
    {
        this.State.SkipNextLeadingTrivia = true;
        return this;
    }

    public CSharpPrintingContext WithTrailingComma(SyntaxTrivia syntaxTrivia, Doc doc)
    {
        this.State.TrailingComma = new TrailingCommaContext(syntaxTrivia, doc);
        return this;
    }

    // Fork (csharpier-editorconfig): extended formatting options driven by .editorconfig.
    // Defaults reproduce upstream CSharpier output exactly.
    public PrintingContextOptions Options { get; init; } = new();

    public class PrintingContextOptions
    {
        public BraceNewLine NewLineBeforeOpenBrace { get; init; } = BraceNewLine.All;
        public bool NewLineBeforeElse { get; init; } = true;
        public bool NewLineBeforeCatch { get; init; } = true;
        public bool NewLineBeforeFinally { get; init; } = true;
        public bool? NewLineBeforeMembersInObjectInitializers { get; init; }
        public bool? NewLineBeforeMembersInAnonymousTypes { get; init; }
        public bool? NewLineBetweenQueryExpressionClauses { get; init; }
        public bool UsePrettierStyleTrailingCommas { get; init; } = true;

        public static PrintingContextOptions From(PrinterOptions printerOptions)
        {
            return new PrintingContextOptions
            {
                NewLineBeforeOpenBrace = printerOptions.NewLineBeforeOpenBrace,
                NewLineBeforeElse = printerOptions.NewLineBeforeElse,
                NewLineBeforeCatch = printerOptions.NewLineBeforeCatch,
                NewLineBeforeFinally = printerOptions.NewLineBeforeFinally,
                NewLineBeforeMembersInObjectInitializers =
                    printerOptions.NewLineBeforeMembersInObjectInitializers,
                NewLineBeforeMembersInAnonymousTypes =
                    printerOptions.NewLineBeforeMembersInAnonymousTypes,
                NewLineBetweenQueryExpressionClauses =
                    printerOptions.NewLineBetweenQueryExpressionClauses,
                UsePrettierStyleTrailingCommas = printerOptions.UsePrettierStyleTrailingCommas,
            };
        }
    }

    public class PrintingContextState
    {
        public int PrintingDepth { get; set; }
        public bool NextTriviaNeedsLine { get; set; }
        public bool SkipNextLeadingTrivia { get; set; }

        // we need to keep track if we reordered modifiers because when modifiers are moved inside
        // of an #if, then we can't compare the before and after disabled text in the source file
        public bool ReorderedModifiers { get; set; }

        // we also need to keep track if we move around usings with disabledText
        public bool ReorderedUsingsWithDisabledText { get; set; }

        public TrailingCommaContext? TrailingComma { get; set; }

        // when adding a trailing comma in front of a trailing comment it is very hard to determine how to compare
        // that trailing comment, so just ignore all trailing trivia
        public bool MovedTrailingTrivia { get; set; }
    }

    public record TrailingCommaContext(SyntaxTrivia TrailingComment, Doc PrintedTrailingComma);
}
