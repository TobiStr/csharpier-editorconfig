---
hide_table_of_contents: true
---

This fork of [belav/csharpier](https://github.com/belav/csharpier) has support for a configuration file. You can use any of the following files
- A ```.csharpierrc``` file in JSON or YAML.
- A ```.csharpierrc.json``` or ```.csharpierrc.yaml``` file.
- A ```.editorconfig``` file. See [EditorConfig](#editorconfig) section below.

The configuration file will be resolved based on the location of the file being formatted.
- If a `.csharpierrc` file exists somewhere at or above the given file, that will be used.
- Otherwise if an `.editorconfig` file exists somewhere at or above the given file, that will be used. Respecting editorconfig inheritance.
### Configuration Options
JSON
```json
{
  "printWidth": 100,
  "useTabs": false,
  "indentSize": 4,
  "endOfLine": "auto",
  "xmlWhitespaceSensitivity": "strict"
}
```
YAML
```yaml
printWidth: 100
useTabs: false
indentSize: 4
endOfLine: auto
xmlWhitespaceSensitivity: strict
```

#### Print Width
Specify at what point the printer will wrap content. This is not a hard limit. Some lines will be shorter or longer.

Default `100`
#### Use Tabs
Indent lines with tabs instead of spaces.

Default `false`
#### Indent Size
Specify the number of spaces used per indentation level.

Default for C# `4`\
Default for XML `2`

#### End of Line

Valid options:

- "auto" - Maintain existing line endings (mixed values within one file are normalised by looking at what's used after the first line)
- "lf" – Line Feed only (\n), common on Linux and macOS as well as inside git repos
- "crlf" - Carriage Return + Line Feed characters (\r\n), common on Windows

Default `auto`

#### Xml Whitespace Sensitivity

CSharpier can deal with whitespace in xml files in two different ways, which will produce different formatting results.

**Strict**
Treats whitespace as significant and will not add/remove new lines at the beginning/end of text elements.

**Ignore** -
Treats whitespace as insignificant and will add/remove new line at the beginning/end of text elements.

For example, given the following xml
```xml
<ElementWithAttribute Attribute="AttributeValue__________________">TextValue</ElementWithAttribute>
```

Strict formatting will break this onto multiple lines, but not add/remove whitespeace around `TextValue`
```xml
<ElementWithAttribute Attribute="AttributeValue__________________"
  >TextValue</ElementWithAttribute>
```

Ignore formatting will add new lines
```xml
<ElementWithAttribute Attribute="AttributeValue__________________">
  TextValue
</ElementWithAttribute>
```

`strict` is the default value for all file extensions except for `xaml` and `axaml`.  
Changing to `ignore` for `csproj` can cause issues because `msbuild` does treat whitespace in the following as significant.
```xml
  <PropertyGroup>
    <MyProperty>
      true
    </MyProperty>
  </PropertyGroup>
  <Target Name="MyTarget" BeforeTargets="Build" Condition=" '$(MyProperty)' == 'true' ">
    <!-- will never be used because '/ntrue/n' != 'true' -->
  </Target>
```

### Configuration Overrides ###
Overrides allows you to specify different configuration options based on glob patterns. This can be used to format non-standard extensions, or to change options based on file path. Top level options will apply to `**/*.{cs,csx}`

```json
{
  "overrides": [
    {
      "files": "*.{csr,cst}",
      "formatter": "csharp",
      "indentSize": 2,
      "useTabs": true,
      "printWidth": 10,
      "endOfLine": "LF"
    }
  ]
}
```

```yaml
overrides:
  - files: "*.{csr,cst}"
    formatter: "csharp"
    indentSize: 2
    useTabs: true
    printWidth: 10
    endOfLine: "LF"
```

### EditorConfig
In addition to the upstream behavior, this fork supports extended configuration via an `.editorconfig` file. A `.csharpierrc*` file in the same directory will take priority.

```ini
[*.{cs,csx}]
# Non-configurable behaviors
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Configurable behaviors
# end_of_line = lf - there is no 'auto' with an .editorconfig
indent_style = space
indent_size = 4
max_line_length = 100
csharpier_include_generated = false
# Optional: control trimming of initial blank lines when formatting
csharpier_trim_initial_lines = true

# Additional C# formatting options
# See https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options
csharp_new_line_before_open_brace = types,methods,control_blocks
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = null
csharp_new_line_before_members_in_anonymous_types = null
csharp_new_line_between_query_expression_clauses = true
csharpier_use_prettier_style_trailing_commas = true
# Force fluent/member-access chains onto one line per link, ignoring max_line_length
csharpier_break_chained_member_access = false
csharpier_break_chained_member_access_minimum_links = 2

[*.{config,csproj,props,slnx,targets,xaml,xml}]
indent_style = space
indent_size = 2
max_line_length = 100
```

#### Breaking chained member access

By default CSharpier keeps a member-access chain on one line whenever it fits within
`max_line_length`. Setting `csharpier_break_chained_member_access = true` forces every
qualifying chain onto one link per line regardless of the line length.

A *link* is one `.` or `?.` access after the root of the chain, so `foo.Bar()` has one link
and `items.Select(...).Where(...).ToList()` has three.
`csharpier_break_chained_member_access_minimum_links` (default `2`) sets how many links a
chain needs before it is broken. The default leaves ordinary accesses such as
`Console.WriteLine(x)` and `string.Empty` untouched. Setting it to `1` additionally breaks
one-link chains that are being assigned; a one-link statement chain has nothing left to
indent, so it stays on one line either way.

A chain that forms a statement keeps its first link on the root line - an expression
statement, an `if` / `while` / `foreach` / `switch` condition, a `return`. A chain whose value
is assigned or passed on - a variable initializer, an assignment, an argument, a lambda body -
starts every link on its own line, so the `=` stays readable. After a `=>` - an expression
bodied member or a lambda body - the root stays on the arrow's line and every link goes below
it.

```csharp
// csharpier_break_chained_member_access = false
var result = items.Select(x => x.Id).Where(x => x > 0).ToList();
services.AddSingleton<IA, A>().AddScoped<IB, B>().AddTransient<IC, C>();

// csharpier_break_chained_member_access = true
var result = items
    .Select(x => x.Id)
    .Where(x => x > 0)
    .ToList();

services.AddSingleton<IA, A>()
    .AddScoped<IB, B>()
    .AddTransient<IC, C>();

if (
    app.IsReady()
        .Check()
) { }

B Build() => builder
    .AddA()
    .AddB();
```

Only the breaks *between* links are forced. Everything inside a link - argument lists,
lambdas, initializers - still follows `max_line_length`. Chains inside an interpolated
string are never broken, because a newline in an interpolation hole does not compile.

These two options are also supported in a `.csharpierrc` file as `breakChainedMemberAccess`
and `breakChainedMemberAccessMinimumLinks`.

Formatting non-standard file extensions using csharpier can be accomplished with the `csharpier_formatter` option
```ini
[*.cst]
csharpier_formatter = csharp
indent_style = space
indent_size = 2
max_line_length = 80
```

