using IniParser.Model;

namespace CSharpier.Cli.EditorConfig;

internal class Section(SectionData section, string directory)
{
    private readonly GlobMatcher matcher = Globber.Create(section.SectionName, directory);
    private readonly GlobMatcher noDirectoryMatcher = Globber.Create(section.SectionName, null);

    public string? IndentStyle { get; } = section.Keys["indent_style"];
    public string? IndentSize { get; } = section.Keys["indent_size"];
    public string? TabWidth { get; } = section.Keys["tab_width"];
    public string? MaxLineLength { get; } = section.Keys["max_line_length"];
    public string? EndOfLine { get; } = section.Keys["end_of_line"];
    public string? Formatter { get; } = section.Keys["csharpier_formatter"];
    public string? XmlWhitespaceSensitivity { get; } =
        section.Keys["csharpier_xml_whitespace_sensitivity"];
    public string? NewLineBeforeOpenBrace { get; } =
        section.Keys["csharp_new_line_before_open_brace"];
    public string? NewLineBeforeElse { get; } = section.Keys["csharp_new_line_before_else"];
    public string? NewLineBeforeCatch { get; } = section.Keys["csharp_new_line_before_catch"];
    public string? NewLineBeforeFinally { get; } = section.Keys["csharp_new_line_before_finally"];
    public string? NewLineBeforeMembersInObjectInitializers { get; } =
        section.Keys["csharp_new_line_before_members_in_object_initializers"];
    public string? NewLineBeforeMembersInAnonymousTypes { get; } =
        section.Keys["csharp_new_line_before_members_in_anonymous_types"];
    public string? NewLineBetweenQueryExpressionClauses { get; } =
        section.Keys["csharp_new_line_between_query_expression_clauses"];
    public string? IncludeGenerated { get; } = section.Keys["csharpier_include_generated"];
    public string? TrimInitialLines { get; } = section.Keys["csharpier_trim_initial_lines"];

    public bool IsMatch(string fileName, bool ignoreDirectory)
    {
        return ignoreDirectory
            ? this.noDirectoryMatcher.IsMatch(fileName)
            : this.matcher.IsMatch(fileName);
    }
}
