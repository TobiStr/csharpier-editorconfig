using CSharpier.Core.DocTypes;
using CSharpier.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpier.Core.CSharp.SyntaxPrinter;

internal static class TrailingComma
{
    public static Doc Print(
        SyntaxToken closingToken,
        PrintingContext context,
        bool skipIfBreak = false
    )
    {
        if (!context.Options.UsePrettierStyleTrailingCommas)
        {
            return Doc.Null;
        }

        var printedToken = Token.Print(SyntaxFactory.Token(SyntaxKind.CommaToken), context);

        return closingToken.LeadingTrivia.Any(o => o.IsDirective) ? Doc.Null
            : skipIfBreak ? printedToken
            : Doc.IfBreak(printedToken, Doc.Null);
    }
}
