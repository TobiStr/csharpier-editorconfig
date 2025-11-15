using CSharpier.Core.DocTypes;
using CSharpier.Core.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.Core.CSharp.SyntaxPrinter.SyntaxNodePrinters;

internal static class TryStatement
{
    public static Doc Print(TryStatementSyntax node, PrintingContext context)
    {
        var docs = new ValueListBuilder<Doc>([null, null, null, null, null, null, null, null]);
        docs.Add(ExtraNewLines.Print(node));
        docs.Add(AttributeLists.Print(node, node.AttributeLists, context));
        docs.Add(Token.Print(node.TryKeyword, context));
        docs.Add(Block.Print(node.Block, context));

        if (node.Catches.Any())
        {
            // First catch: controlled by NewLineBeforeCatch, subsequent always on new lines.
            var firstCatch = node.Catches.First();
            docs.Add(
                context.Options.NewLineBeforeCatch ? Doc.HardLine : " ",
                CatchClause.Print(firstCatch, context)
            );

            foreach (var remaining in node.Catches.Skip(1))
            {
                docs.Add(Doc.HardLine, CatchClause.Print(remaining, context));
            }
        }

        if (node.Finally != null)
        {
            docs.Add(
                context.Options.NewLineBeforeFinally ? Doc.HardLine : " ",
                FinallyClause.Print(node.Finally, context)
            );
        }
        return Doc.Concat(ref docs);
    }
}
