using CSharpier.Core;

namespace CSharpier.Cli.EditorConfig;

/// <summary>
/// This is a representation of the editorconfig for the given directory along with
/// sections from any parent files until a root file is found
/// </summary>
internal class EditorConfigSections
{
    public required string DirectoryName { get; init; }
    public required IReadOnlyCollection<Section> SectionsIncludingParentFiles { get; init; }

    public PrinterOptions? ConvertToPrinterOptions(string filePath, bool ignoreDirectory)
    {
        var sections = this
            .SectionsIncludingParentFiles.Where(o => o.IsMatch(filePath, ignoreDirectory))
            .ToList();
        var resolvedConfiguration = new ResolvedConfiguration(sections);

        var formatter =
            resolvedConfiguration.Formatter ?? PrinterOptions.GetFormatter(filePath).ToString();

        if (!Enum.TryParse<Formatter>(formatter, ignoreCase: true, out var parsedFormatter))
        {
            return null;
        }

        var printerOptions = new PrinterOptions(
            parsedFormatter,
            PrinterOptions.GetXmlWhitespaceSensitivity(filePath)
        )
        {
            TrimInitialLines = resolvedConfiguration.TrimInitialLines ?? true,
        };

        if (resolvedConfiguration.MaxLineLength is { } maxLineLength)
        {
            printerOptions.Width = maxLineLength;
        }

        if (resolvedConfiguration.IndentStyle is "tab")
        {
            printerOptions.UseTabs = true;
        }

        if (printerOptions.UseTabs)
        {
            printerOptions.IndentSize = resolvedConfiguration.TabWidth ?? printerOptions.IndentSize;
        }
        else
        {
            printerOptions.IndentSize =
                resolvedConfiguration.IndentSize ?? printerOptions.IndentSize;
        }

        if (resolvedConfiguration.EndOfLine is { } endOfLine)
        {
            printerOptions.EndOfLine = endOfLine;
        }

        if (resolvedConfiguration.NewLineBeforeOpenBrace is { } newLineBeforeOpenBrace)
        {
            printerOptions.NewLineBeforeOpenBrace = newLineBeforeOpenBrace;
        }

        if (resolvedConfiguration.NewLineBeforeElse is { } newLineBeforeElse)
        {
            printerOptions.NewLineBeforeElse = newLineBeforeElse;
        }

        if (resolvedConfiguration.NewLineBeforeCatch is { } newLineBeforeCatch)
        {
            printerOptions.NewLineBeforeCatch = newLineBeforeCatch;
        }

        if (resolvedConfiguration.NewLineBeforeFinally is { } newLineBeforeFinally)
        {
            printerOptions.NewLineBeforeFinally = newLineBeforeFinally;
        }

        if (
            resolvedConfiguration.NewLineBeforeMembersInObjectInitializers is
            { } newLineBeforeMembersInObjectInitializers
        )
        {
            printerOptions.NewLineBeforeMembersInObjectInitializers =
                newLineBeforeMembersInObjectInitializers;
        }

        if (
            resolvedConfiguration.NewLineBeforeMembersInAnonymousTypes is
            { } newLineBeforeMembersInAnonymousTypes
        )
        {
            printerOptions.NewLineBeforeMembersInAnonymousTypes =
                newLineBeforeMembersInAnonymousTypes;
        }

        if (
            resolvedConfiguration.NewLineBetweenQueryExpressionClauses is
            { } newLineBetweenQueryExpressionClauses
        )
        {
            printerOptions.NewLineBetweenQueryExpressionClauses =
                newLineBetweenQueryExpressionClauses;
        }

        if (
            resolvedConfiguration.UsePrettierStyleTrailingCommas is
            { } usePrettierStyleTrailingCommas
        )
        {
            printerOptions.UsePrettierStyleTrailingCommas = usePrettierStyleTrailingCommas;
        }

        if (resolvedConfiguration.BreakChainedMemberAccess is { } breakChainedMemberAccess)
        {
            printerOptions.BreakChainedMemberAccess = breakChainedMemberAccess;
        }

        if (
            resolvedConfiguration.BreakChainedMemberAccessMinimumLinks is
            { } breakChainedMemberAccessMinimumLinks
        )
        {
            printerOptions.BreakChainedMemberAccessMinimumLinks =
                breakChainedMemberAccessMinimumLinks;
        }

        if (resolvedConfiguration.IncludeGenerated is { } includeGenerated)
        {
            printerOptions.IncludeGenerated = includeGenerated;
        }

        if (resolvedConfiguration.XmlWhitespaceSensitivity is { } xmlWhitespaceSensitivity)
        {
            printerOptions.XmlWhitespaceSensitivity = xmlWhitespaceSensitivity;
        }

        return printerOptions;
    }

    private class ResolvedConfiguration
    {
        public string? IndentStyle { get; }
        public int? IndentSize { get; }
        public int? TabWidth { get; }
        public int? MaxLineLength { get; }
        public EndOfLine? EndOfLine { get; }
        public XmlWhitespaceSensitivity? XmlWhitespaceSensitivity { get; set; }
        public string? Formatter { get; }
        public BraceNewLine? NewLineBeforeOpenBrace { get; }
        public bool? NewLineBeforeElse { get; }
        public bool? NewLineBeforeCatch { get; }
        public bool? NewLineBeforeFinally { get; }
        public bool? NewLineBeforeMembersInObjectInitializers { get; }
        public bool? NewLineBeforeMembersInAnonymousTypes { get; }
        public bool? NewLineBetweenQueryExpressionClauses { get; }
        public bool? UsePrettierStyleTrailingCommas { get; }
        public bool? BreakChainedMemberAccess { get; }
        public int? BreakChainedMemberAccessMinimumLinks { get; }
        public bool? IncludeGenerated { get; }
        public bool? TrimInitialLines { get; }

        public ResolvedConfiguration(List<Section> sections)
        {
            var indentStyle = sections.LastOrDefault(o => o.IndentStyle != null)?.IndentStyle;
            if (indentStyle is "space" or "tab")
            {
                this.IndentStyle = indentStyle;
            }

            var maxLineLength = sections.LastOrDefault(o => o.MaxLineLength != null)?.MaxLineLength;
            if (int.TryParse(maxLineLength, out var maxLineLengthValue) && maxLineLengthValue > 0)
            {
                this.MaxLineLength = maxLineLengthValue;
            }

            var indentSize = sections.LastOrDefault(o => o.IndentSize != null)?.IndentSize;
            var tabWidth = sections.LastOrDefault(o => o.TabWidth != null)?.TabWidth;

            if (indentSize == "tab")
            {
                if (int.TryParse(tabWidth, out var tabWidthValue))
                {
                    this.TabWidth = tabWidthValue;
                }

                this.IndentSize = this.TabWidth;
            }
            else
            {
                if (int.TryParse(indentSize, out var indentSizeValue))
                {
                    this.IndentSize = indentSizeValue;
                }

                this.TabWidth = int.TryParse(tabWidth, out var tabWidthValue)
                    ? tabWidthValue
                    : this.IndentSize;
            }

            var endOfLine = sections.LastOrDefault(o => o.EndOfLine != null)?.EndOfLine;
            if (Enum.TryParse(endOfLine, true, out EndOfLine parsedEndOfLine))
            {
                this.EndOfLine = parsedEndOfLine;
            }

            var xmlWhitespaceSensitivity = sections
                .LastOrDefault(o => o.XmlWhitespaceSensitivity != null)
                ?.XmlWhitespaceSensitivity;
            if (
                Enum.TryParse(
                    xmlWhitespaceSensitivity,
                    true,
                    out XmlWhitespaceSensitivity parsedXmlWhitespaceSensitivity
                )
            )
            {
                this.XmlWhitespaceSensitivity = parsedXmlWhitespaceSensitivity;
            }

            this.Formatter = sections.LastOrDefault(o => o.Formatter is not null)?.Formatter;

            var newLineBeforeOpenBrace = sections
                .LastOrDefault(o => o.NewLineBeforeOpenBrace != null)
                ?.NewLineBeforeOpenBrace;
            if (!string.IsNullOrWhiteSpace(newLineBeforeOpenBrace))
            {
                this.NewLineBeforeOpenBrace = ConvertToBraceNewLine(newLineBeforeOpenBrace);
            }

            var includeGenerated = sections
                .LastOrDefault(o => o.IncludeGenerated != null)
                ?.IncludeGenerated;
            if (bool.TryParse(includeGenerated, out var includeGeneratedValue))
            {
                this.IncludeGenerated = includeGeneratedValue;
            }

            var trimInitialLines = sections
                .LastOrDefault(o => o.TrimInitialLines != null)
                ?.TrimInitialLines;
            if (bool.TryParse(trimInitialLines, out var trimInitialLinesValue))
            {
                this.TrimInitialLines = trimInitialLinesValue;
            }

            var newLineBeforeElse = sections
                .LastOrDefault(o => o.NewLineBeforeElse != null)
                ?.NewLineBeforeElse;
            if (bool.TryParse(newLineBeforeElse, out var newLineBeforeElseValue))
            {
                this.NewLineBeforeElse = newLineBeforeElseValue;
            }

            var newLineBeforeCatch = sections
                .LastOrDefault(o => o.NewLineBeforeCatch != null)
                ?.NewLineBeforeCatch;
            if (bool.TryParse(newLineBeforeCatch, out var newLineBeforeCatchValue))
            {
                this.NewLineBeforeCatch = newLineBeforeCatchValue;
            }

            var newLineBeforeFinally = sections
                .LastOrDefault(o => o.NewLineBeforeFinally != null)
                ?.NewLineBeforeFinally;
            if (bool.TryParse(newLineBeforeFinally, out var newLineBeforeFinallyValue))
            {
                this.NewLineBeforeFinally = newLineBeforeFinallyValue;
            }

            var newLineBeforeMembersInObjectInitializers = sections
                .LastOrDefault(o => o.NewLineBeforeMembersInObjectInitializers != null)
                ?.NewLineBeforeMembersInObjectInitializers;
            if (
                !string.IsNullOrWhiteSpace(newLineBeforeMembersInObjectInitializers)
                && bool.TryParse(
                    newLineBeforeMembersInObjectInitializers,
                    out var newLineBeforeMembersInObjectInitializersValue
                )
            )
            {
                this.NewLineBeforeMembersInObjectInitializers =
                    newLineBeforeMembersInObjectInitializersValue;
            }

            var newLineBeforeMembersInAnonymousTypes = sections
                .LastOrDefault(o => o.NewLineBeforeMembersInAnonymousTypes != null)
                ?.NewLineBeforeMembersInAnonymousTypes;
            if (
                !string.IsNullOrWhiteSpace(newLineBeforeMembersInAnonymousTypes)
                && bool.TryParse(
                    newLineBeforeMembersInAnonymousTypes,
                    out var newLineBeforeMembersInAnonymousTypesValue
                )
            )
            {
                this.NewLineBeforeMembersInAnonymousTypes =
                    newLineBeforeMembersInAnonymousTypesValue;
            }

            var newLineBetweenQueryExpressionClauses = sections
                .LastOrDefault(o => o.NewLineBetweenQueryExpressionClauses != null)
                ?.NewLineBetweenQueryExpressionClauses;
            if (
                !string.IsNullOrWhiteSpace(newLineBetweenQueryExpressionClauses)
                && bool.TryParse(
                    newLineBetweenQueryExpressionClauses,
                    out var newLineBetweenQueryExpressionClausesValue
                )
            )
            {
                this.NewLineBetweenQueryExpressionClauses =
                    newLineBetweenQueryExpressionClausesValue;
            }

            var usePrettierStyleTrailingCommas = sections
                .LastOrDefault(o => o.UsePrettierStyleTrailingCommas != null)
                ?.UsePrettierStyleTrailingCommas;
            if (
                bool.TryParse(
                    usePrettierStyleTrailingCommas,
                    out var usePrettierStyleTrailingCommasValue
                )
            )
            {
                this.UsePrettierStyleTrailingCommas = usePrettierStyleTrailingCommasValue;
            }

            var breakChainedMemberAccess = sections
                .LastOrDefault(o => o.BreakChainedMemberAccess != null)
                ?.BreakChainedMemberAccess;
            if (bool.TryParse(breakChainedMemberAccess, out var breakChainedMemberAccessValue))
            {
                this.BreakChainedMemberAccess = breakChainedMemberAccessValue;
            }

            var breakChainedMemberAccessMinimumLinks = sections
                .LastOrDefault(o => o.BreakChainedMemberAccessMinimumLinks != null)
                ?.BreakChainedMemberAccessMinimumLinks;
            if (
                int.TryParse(
                    breakChainedMemberAccessMinimumLinks,
                    out var breakChainedMemberAccessMinimumLinksValue
                )
                && breakChainedMemberAccessMinimumLinksValue >= 1
            )
            {
                this.BreakChainedMemberAccessMinimumLinks =
                    breakChainedMemberAccessMinimumLinksValue;
            }
        }
    }

    internal static BraceNewLine ConvertToBraceNewLine(string input)
    {
        var result = BraceNewLine.None;
        var values = input.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );

        foreach (var value in values)
        {
            var enumValueName = value.Replace("_", "");
            foreach (var enumValue in Enum.GetValues<BraceNewLine>())
            {
                if (
                    string.Equals(
                        enumValue.ToString(),
                        enumValueName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    result |= enumValue;
                    break;
                }
            }
        }

        return result;
    }
}
