using CSharpier.Core.DocTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.Core.CSharp.SyntaxPrinter.SyntaxNodePrinters;

internal static class Block
{
    public static Doc Print(BlockSyntax node, CSharpPrintingContext context)
    {
        if (
            node.Statements.Count == 0
            && node.Parent is MethodDeclarationSyntax
            && !Token.HasComments(node.CloseBraceToken)
        )
        {
            return Doc.Concat(
                " ",
                Token.Print(node.OpenBraceToken, context),
                " ",
                Token.Print(node.CloseBraceToken, context)
            );
        }

        Doc statementSeparator =
            node.Parent is AccessorDeclarationSyntax && node.Statements.Count <= 1
                ? Doc.Line
                : Doc.HardLine;

        Doc innerDoc = Doc.Null;

        if (node.Statements.Count > 0)
        {
            var statements = CSharpierIgnore.PrintNodesRespectingRangeIgnore(
                node.Statements,
                context
            );

            innerDoc = Doc.Indent(statementSeparator, Doc.Join(statementSeparator, statements));

            DocUtilities.RemoveInitialDoubleHardLine(innerDoc);
        }

        Doc leading;
        if (
            node.Parent
            is ParenthesizedLambdaExpressionSyntax
                or BlockSyntax
                or GlobalStatementSyntax
        )
        {
            leading = Doc.Null;
        }
        else if (node.Parent is BaseMethodDeclarationSyntax)
        {
            leading = context.Options.NewLineBeforeOpenBrace.HasFlag(BraceNewLine.Methods)
                ? Doc.Line
                : " ";
        }
        else if (
            node.Parent
            is IfStatementSyntax
                or ElseClauseSyntax
                or ForStatementSyntax
                or ForEachStatementSyntax
                or WhileStatementSyntax
                or TryStatementSyntax
                or CatchClauseSyntax
                or FinallyClauseSyntax
        )
        {
            leading = context.Options.NewLineBeforeOpenBrace.HasFlag(BraceNewLine.ControlBlocks)
                ? Doc.Line
                : " ";
        }
        else
        {
            leading = Doc.Line;
        }

        var result = Doc.Group(
            leading,
            Token.Print(node.OpenBraceToken, context),
            node.Statements.Count == 0 ? " " : Doc.Concat(innerDoc, statementSeparator),
            Token.Print(node.CloseBraceToken, context)
        );

        return node.Parent is BlockSyntax ? Doc.Concat(ExtraNewLines.Print(node), result) : result;
    }
}
