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

    private const int BreakChainWidth = 200;

    private static string FormatWithChainBreaking(
        string code,
        bool breakChainedMemberAccess = true,
        int minimumLinks = 2
    )
    {
        return CSharpFormatter
            .Format(
                code,
                new CodeFormatterOptions
                {
                    Width = BreakChainWidth,
                    BreakChainedMemberAccess = breakChainedMemberAccess,
                    BreakChainedMemberAccessMinimumLinks = minimumLinks,
                }
            )
            .Code;
    }

    [Test]
    public void Format_Should_Not_Break_Chains_By_Default()
    {
        var code = "var result = items.Select(x => x.Id).Where(x => x > 0).ToList();";

        var result = CSharpFormatter.Format(code, new CodeFormatterOptions { Width = 200 });

        result
            .Code.Should()
            .Be("var result = items.Select(x => x.Id).Where(x => x > 0).ToList();\n");
    }

    [Test]
    public void Format_Should_Break_Chains_When_Enabled()
    {
        var code = "var result = items.Select(x => x.Id).Where(x => x > 0).ToList();";

        var result = FormatWithChainBreaking(code);

        result
            .Should()
            .Be(
                "var result = items\n    .Select(x => x.Id)\n    .Where(x => x > 0)\n    .ToList();\n"
            );
    }

    [Test]
    public void Format_Should_Not_Break_Single_Link_Chain_With_Default_MinimumLinks()
    {
        var result = FormatWithChainBreaking("foo.Bar();");

        result.Should().Be("foo.Bar();\n");
    }

    [Test]
    public void Format_Should_Not_Break_Single_Link_Statement_Chain_When_MinimumLinks_Is_One()
    {
        var result = FormatWithChainBreaking("foo.Bar();", minimumLinks: 1);

        result.Should().Be("foo.Bar();\n");
    }

    [Test]
    public void Format_Should_Break_Property_Chain()
    {
        var result = FormatWithChainBreaking("var name = user.Profile.Contact.Name;");

        result.Should().Be("var name = user\n    .Profile\n    .Contact\n    .Name;\n");
    }

    [Test]
    public void Format_Should_Break_Mixed_Property_And_Method_Chain()
    {
        var result = FormatWithChainBreaking("request.User.Profile.Name.ToLowerInvariant();");

        result.Should().Be("request.User\n    .Profile\n    .Name\n    .ToLowerInvariant();\n");
    }

    [Test]
    public void Format_Should_Break_Chain_Rooted_In_An_Invocation()
    {
        var result = FormatWithChainBreaking("GetService().Foo().Bar();");

        result.Should().Be("GetService().Foo()\n    .Bar();\n");
    }

    [Test]
    public void Format_Should_Break_Chain_Rooted_In_A_Parenthesized_Expression()
    {
        var result = FormatWithChainBreaking("(a + b).Foo().Bar();");

        result.Should().Be("(a + b).Foo()\n    .Bar();\n");
    }

    [Test]
    public void Format_Should_Break_Chain_After_Await()
    {
        var result = FormatWithChainBreaking(
            "var x = await items.Where(o => o.Enabled).ToListAsync();"
        );

        result
            .Should()
            .Be("var x = await items\n    .Where(o => o.Enabled)\n    .ToListAsync();\n");
    }

    [Test]
    public void Format_Should_Keep_NullConditional_Operator_With_Its_Member()
    {
        var result = FormatWithChainBreaking("foo?.Bar().Baz();");

        result.Should().Be("foo?.Bar()\n    .Baz();\n");
    }

    [Test]
    public void Format_Should_Keep_NullForgiving_Operator_With_The_Root()
    {
        var result = FormatWithChainBreaking("foo!.Bar().Baz();");

        result.Should().Be("foo!.Bar()\n    .Baz();\n");
    }

    [Test]
    public void Format_Should_Break_Chain_With_Generic_Method_Call()
    {
        var result = FormatWithChainBreaking("foo.Get<int>().Bar();");

        result.Should().Be("foo.Get<int>()\n    .Bar();\n");
    }

    [Test]
    public void Format_Should_Break_Chain_Nested_In_An_Argument()
    {
        var result = FormatWithChainBreaking("Outer(items.Where(x => x.A).ToList());");

        result.Should().Be("Outer(\n    items\n        .Where(x => x.A)\n        .ToList()\n);\n");
    }

    [Test]
    public void Format_Should_Still_Wrap_Arguments_Using_PrintWidth()
    {
        var code = "items.Where(x => SomeLongCondition(x, foo, bar)).Select(x => x.Value);";

        var result = CSharpFormatter
            .Format(
                code,
                new CodeFormatterOptions
                {
                    Width = 40,
                    BreakChainedMemberAccess = true,
                    BreakChainedMemberAccessMinimumLinks = 2,
                }
            )
            .Code;

        result
            .Should()
            .Be(
                "items.Where(x =>\n        SomeLongCondition(x, foo, bar)\n    )\n    .Select(x => x.Value);\n"
            );
    }

    [Test]
    public void Format_Should_Merge_First_Link_For_A_Statement_Chain()
    {
        var code = "services.AddSingleton<IA, A>().AddScoped<IB, B>().AddTransient<IC, C>();";

        var result = FormatWithChainBreaking(code);

        result
            .Should()
            .Be(
                "services.AddSingleton<IA, A>()\n    .AddScoped<IB, B>()\n    .AddTransient<IC, C>();\n"
            );
    }

    [Test]
    public void Format_Should_Not_Merge_First_Link_When_Assigned()
    {
        var result = FormatWithChainBreaking("abc = obj.Select(x => x.Id).Where(x => x > 0);");

        result.Should().Be("abc = obj\n    .Select(x => x.Id)\n    .Where(x => x > 0);\n");
    }

    [Test]
    public void Format_Should_Merge_First_Link_In_An_If_Condition()
    {
        var result = FormatWithChainBreaking("if (app.IsReady().Check()) { }");

        result.Should().Be("if (\n    app.IsReady()\n        .Check()\n) { }\n");
    }

    [Test]
    public void Format_Should_Merge_First_Link_In_A_While_Condition()
    {
        var result = FormatWithChainBreaking("while (queue.Peek().IsValid()) { }");

        result.Should().Be("while (\n    queue.Peek()\n        .IsValid()\n) { }\n");
    }

    [Test]
    public void Format_Should_Merge_First_Link_In_A_Foreach_Expression()
    {
        var code = "foreach (var x in items.Where(o => o.A).Select(o => o.B)) { }";

        var result = FormatWithChainBreaking(code);

        result
            .Should()
            .Be(
                "foreach (\n    var x in items.Where(o => o.A)\n        .Select(o => o.B)\n) { }\n"
            );
    }

    [Test]
    public void Format_Should_Merge_First_Link_In_A_Return_Statement()
    {
        var result = FormatWithChainBreaking("return builder.AddA().AddB();");

        result.Should().Be("return builder.AddA()\n    .AddB();\n");
    }

    [Test]
    public void Format_Should_Merge_First_Link_For_An_Awaited_Statement_Chain()
    {
        var result = FormatWithChainBreaking("await app.StartAsync().ConfigureAwait(false);");

        result.Should().Be("await app.StartAsync()\n    .ConfigureAwait(false);\n");
    }

    [Test]
    public void Format_Should_Keep_Root_On_The_Arrow_Line_For_An_Expression_Bodied_Member()
    {
        var result = FormatWithChainBreaking("class C { B N() => builder.AddA().AddB(); }");

        result
            .Should()
            .Be("class C\n{\n    B N() => builder\n        .AddA()\n        .AddB();\n}\n");
    }

    [Test]
    public void Format_Should_Still_Break_After_The_Arrow_When_The_Body_Does_Not_Fit()
    {
        var code =
            "class C { string LongOne => someRatherLongReceiverName.SingleLinkMethod(arg); }";

        var result = CSharpFormatter
            .Format(code, new CodeFormatterOptions { Width = 60, BreakChainedMemberAccess = true })
            .Code;

        result
            .Should()
            .Be(
                "class C\n{\n    string LongOne =>\n        someRatherLongReceiverName.SingleLinkMethod(arg);\n}\n"
            );
    }

    [Test]
    public void Format_Should_Keep_Root_On_The_Arrow_Line_For_A_Parenthesized_Lambda()
    {
        var result = FormatWithChainBreaking("var f = (X x) => x.GetA().GetB();");

        result.Should().Be("var f = (X x) => x\n    .GetA()\n    .GetB();\n");
    }

    [Test]
    public void Format_Should_Keep_Root_On_The_Arrow_Line_For_A_Simple_Lambda()
    {
        var result = FormatWithChainBreaking("var g = x => x.GetA().GetB();");

        result.Should().Be("var g = x => x\n    .GetA()\n    .GetB();\n");
    }

    [Test]
    public void Format_Should_Not_Change_A_Lambda_Body_That_Is_Not_A_Chain()
    {
        var code = "items.Where(x => SomeCondition(x, foo, bar)).Select(x => x.Value);";

        var result = FormatWithChainBreaking(code);

        result
            .Should()
            .Be("items.Where(x => SomeCondition(x, foo, bar))\n    .Select(x => x.Value);\n");
    }

    [Test]
    public void Format_Should_Not_Break_Chain_Inside_An_Interpolated_String()
    {
        var result = FormatWithChainBreaking("var x = $\"{foo.Bar().Baz()}\";", minimumLinks: 1);

        result.Should().Be("var x = $\"{foo.Bar().Baz()}\";\n");
    }

    [Test]
    public void Format_Should_Keep_Comments_Attached_To_Their_Chain_Link()
    {
        var code = "var x = items\n// a comment\n.Where(o => o.A)\n.ToList();";

        var result = FormatWithChainBreaking(code);

        result
            .Should()
            .Be("var x = items\n    // a comment\n    .Where(o => o.A)\n    .ToList();\n");
    }
}
