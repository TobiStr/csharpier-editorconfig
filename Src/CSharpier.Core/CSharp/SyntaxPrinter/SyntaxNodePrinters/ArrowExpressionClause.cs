using CSharpier.Core.DocTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.Core.CSharp.SyntaxPrinter.SyntaxNodePrinters;

internal static class ArrowExpressionClause
{
    public static Doc Print(ArrowExpressionClauseSyntax node, CSharpPrintingContext context)
    {
        // Fork (csharpier-editorconfig): with csharpier_break_chained_member_access a chain body
        // always contains a hard line. In the layout below that break propagates to the whole
        // group and pushes the chain onto the line after the "=>". Giving the line its own group
        // stops the propagation, so the root stays on the "=>" line and only the links break -
        // the same shape an assignment already gets from RightHandSide's fluid layout.
        if (InvocationExpression.WillBreakChain(node.Expression, context))
        {
            return Doc.Group(
                " ",
                Token.Print(node.ArrowToken, context),
                InvocationExpression.PrintChainAfterOperator(node.Expression, context)
            );
        }

        return Doc.Group(
            Doc.Indent(
                " ",
                Token.PrintWithSuffix(node.ArrowToken, Doc.Line, context),
                Node.Print(node.Expression, context)
            )
        );
    }
}
