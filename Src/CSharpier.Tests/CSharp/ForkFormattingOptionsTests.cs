using AwesomeAssertions;
using CSharpier.Core;
using CSharpier.Core.CSharp;

namespace CSharpier.Tests.CSharp;

/// <summary>
/// Formatting tests for the options this fork (csharpier-editorconfig) adds on top of
/// upstream CSharpier. Kept in a fork-owned file so upstream merges do not conflict.
/// </summary>
internal sealed class ForkFormattingOptionsTests
{
    [Test]
    public void Format_Should_Print_Trailing_Comma_By_Default()
    {
        var code = "var x = new Foo { Apple = 1, Banana = 2 };";

        var result = CSharpFormatter.Format(code, new CodeFormatterOptions { Width = 30 });

        result.Code.Should().Be("var x = new Foo\n{\n    Apple = 1,\n    Banana = 2,\n};\n");
    }

    [Test]
    public void Format_Should_Not_Print_Trailing_Comma_When_Disabled()
    {
        var code = "var x = new Foo { Apple = 1, Banana = 2 };";

        var result = CSharpFormatter.Format(
            code,
            new CodeFormatterOptions { Width = 30, UsePrettierStyleTrailingCommas = false }
        );

        result.Code.Should().Be("var x = new Foo\n{\n    Apple = 1,\n    Banana = 2\n};\n");
    }
}
