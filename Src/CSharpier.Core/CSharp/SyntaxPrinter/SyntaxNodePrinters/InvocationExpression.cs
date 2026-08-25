using System.Diagnostics.CodeAnalysis;
using CSharpier.Core.DocTypes;
using CSharpier.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.Core.CSharp.SyntaxPrinter.SyntaxNodePrinters;

internal record PrintedNode(CSharpSyntaxNode Node, Doc Doc);

// This is based on prettier/src/language-js/print/member-chain.js
// various discussions/prs about how to potentially improve the formatting
// https://github.com/prettier/prettier/issues/5737
// https://github.com/prettier/prettier/issues/7884
// https://github.com/prettier/prettier/issues/8003
// https://github.com/prettier/prettier/issues/8902
// https://github.com/prettier/prettier/pull/7889
internal static class InvocationExpression
{
    public static Doc Print(InvocationExpressionSyntax node, CSharpPrintingContext context)
    {
        return PrintMemberChain(node, context);
    }

    public static Doc PrintMemberChain(ExpressionSyntax node, CSharpPrintingContext context)
    {
        var parent = node.Parent;
        var printedNodes = new List<PrintedNode>();

        FlattenAndPrintNodes(node, printedNodes, context);

        var groups = printedNodes.Any(o => o.Node is InvocationExpressionSyntax)
            ? GroupPrintedNodesPrettierStyle(printedNodes)
            : GroupPrintedNodesOnLines(printedNodes);

        // Fork (csharpier-editorconfig): csharpier_break_chained_member_access forces one chain
        // link per line regardless of the print width. GroupPrintedNodesOnLines already produces
        // exactly that shape, so it is reused instead of adding a third grouping function.
        var breakChain = false;
        if (context.Options.BreakChainedMemberAccess)
        {
            var perLinkGroups = GroupPrintedNodesOnLines(printedNodes);
            breakChain =
                perLinkGroups.Count >= 2
                && perLinkGroups.Count - 1 >= context.Options.BreakChainedMemberAccessMinimumLinks
                // a hard line inside an interpolation hole of a non-raw string does not compile
                && !node.HasParent(typeof(InterpolatedStringExpressionSyntax));
            if (breakChain)
            {
                groups = perLinkGroups;
            }
        }

        var oneLine = SelectManyDocsToArray(groups);

        var shouldMergeFirstTwoGroups = breakChain
            ? IsStatementChain(parent)
            : ShouldMergeFirstTwoGroups(groups, parent);

        var cutoff = shouldMergeFirstTwoGroups ? 3 : 2;

        var forceOneLine =
            (
                groups.Count <= cutoff
                && (
                    groups
                        .Skip(shouldMergeFirstTwoGroups ? 1 : 0)
                        .Any(o =>
                            o.Last().Node
                                is not (
                                    InvocationExpressionSyntax
                                    or ElementAccessExpressionSyntax
                                    or PostfixUnaryExpressionSyntax
                                    {
                                        Operand: InvocationExpressionSyntax
                                    }
                                )
                        )
                    // if the last group contains just a !, make sure it doesn't end up on a new line
                    || (
                        groups.Last().Count == 1
                        && groups.Last()[0].Node is PostfixUnaryExpressionSyntax
                    )
                )
            )
            || node.HasParent(typeof(InterpolatedStringExpressionSyntax))
            // this handles the case of a multiline string being part of an invocation chain
            // conditional groups don't propagate breaks so we need to avoid the conditional group
            || groups[0]
                .Any(o =>
                    o.Node
                        is LiteralExpressionSyntax
                            {
                                Token.RawKind: (int)SyntaxKind.MultiLineRawStringLiteralToken
                            }
                            or InterpolatedStringExpressionSyntax
                            {
                                StringStartToken.RawKind: (int)
                                    SyntaxKind.InterpolatedMultiLineRawStringStartToken
                            }
                    || o.Node
                        is LiteralExpressionSyntax
                        {
                            Token.Text.Length: > 0
                        } literalExpressionSyntax
                        && literalExpressionSyntax.Token.Text.Contains('\n')
                );

        if (!breakChain && forceOneLine)
        {
            return Doc.Group(oneLine);
        }

        var expanded = Doc.Concat(
            Doc.Concat(groups[0].Select(o => o.Doc).ToArray()),
            shouldMergeFirstTwoGroups
                ? Doc.IndentIf(
                    groups.Count > 2 && groups[1].Last().Doc is not Group { Contents: IndentDoc },
                    Doc.Concat(groups[1].Select(o => o.Doc).ToArray())
                )
                : Doc.Null,
            PrintIndentedGroup(groups.Skip(shouldMergeFirstTwoGroups ? 2 : 1).ToList())
        );

        return
            breakChain
            || oneLine.Skip(1).Any(DocUtilities.ContainsBreak)
            || groups[0]
                .Any(o =>
                    o.Node
                        is ArrayCreationExpressionSyntax
                            or ObjectCreationExpressionSyntax { Initializer: not null }
                )
            || groups[0].First().Node
                is ParenthesizedExpressionSyntax
                {
                    Expression: SwitchExpressionSyntax or QueryExpressionSyntax
                }
            || (
                parent is ExpressionStatementSyntax expressionStatementSyntax
                && expressionStatementSyntax.SemicolonToken.LeadingTrivia.Any(o => o.IsComment())
            )
            || groups.Count == 1
            ? expanded
            : Doc.ConditionalGroup(Doc.Concat(oneLine), expanded);
    }

    // Fork (csharpier-editorconfig): true when csharpier_break_chained_member_access will force
    // this expression onto several lines, so callers such as ArrowExpressionClause can lay out
    // the operator accordingly. Counting links here mirrors GroupPrintedNodesOnLines without
    // printing anything - PrintMemberChain cannot be reused for the check because flattening a
    // chain prints every node and would both duplicate that work and disturb context.State.
    internal static bool WillBreakChain(ExpressionSyntax expression, CSharpPrintingContext context)
    {
        return context.Options.BreakChainedMemberAccess
            && CountChainLinks(expression)
                >= Math.Max(context.Options.BreakChainedMemberAccessMinimumLinks, 1)
            // a hard line inside an interpolation hole of a non-raw string does not compile
            && !expression.HasParent(typeof(InterpolatedStringExpressionSyntax));
    }

    // The number of '.' / '?.' accesses on the chain's spine, which is what
    // GroupPrintedNodesOnLines turns into one group each after the root.
    private static int CountChainLinks(ExpressionSyntax expression)
    {
        return expression switch
        {
            InvocationExpressionSyntax invocation => CountChainLinks(invocation.Expression),
            ElementAccessExpressionSyntax elementAccess => CountChainLinks(
                elementAccess.Expression
            ),
            PostfixUnaryExpressionSyntax
            {
                Operand: InvocationExpressionSyntax or MemberAccessExpressionSyntax
            } postfixUnary => CountChainLinks(postfixUnary.Operand),
            MemberAccessExpressionSyntax memberAccess => CountChainLinks(memberAccess.Expression)
                + 1,
            ConditionalAccessExpressionSyntax conditionalAccess => CountChainLinks(
                conditionalAccess.Expression
            ) + CountChainLinks(conditionalAccess.WhenNotNull),
            MemberBindingExpressionSyntax or ElementBindingExpressionSyntax => 1,
            _ => 0,
        };
    }

    // Fork (csharpier-editorconfig): prints a chain that follows an operator such as "=>" while
    // keeping its root on the operator's line. A broken chain contains a hard line, which would
    // otherwise propagate to the enclosing group and push the whole chain onto the next line.
    // Giving the line its own group stops that propagation, and the group still breaks when the
    // body genuinely does not fit - the same shape RightHandSide's fluid layout gives an "=".
    internal static Doc PrintChainAfterOperator(
        ExpressionSyntax expression,
        CSharpPrintingContext context
    )
    {
        var groupId = context.GroupFor("ChainAfterOperator");

        return Doc.Concat(
            Doc.GroupWithId(groupId, Doc.Indent(Doc.Line)),
            Doc.IndentIfBreak(Node.Print(expression, context), groupId)
        );
    }

    private static void FlattenAndPrintNodes(
        ExpressionSyntax expression,
        List<PrintedNode> printedNodes,
        CSharpPrintingContext context
    )
    {
        /*
          We need to flatten things out because the AST has them this way
          InvocationExpression
          Expression                        ArgumentList
          this.DoSomething().DoSomething    ()
          
          MemberAccessExpression
          Expression            OperatorToken   Name
          this.DoSomething()    .               DoSomething
          
          InvocationExpression
          Expression            ArgumentList
          this.DoSomething      ()
          
          MemberAccessExpression
          Expression    OperatorToken   Name
          this          .               DoSomething
          
          And we want to work with them from Left to Right
        */
        if (expression is InvocationExpressionSyntax invocationExpressionSyntax)
        {
            FlattenAndPrintNodes(invocationExpressionSyntax.Expression, printedNodes, context);
            printedNodes.Add(
                new PrintedNode(
                    invocationExpressionSyntax,
                    ArgumentList.Print(invocationExpressionSyntax.ArgumentList, context)
                )
            );
        }
        else if (expression is ElementAccessExpressionSyntax elementAccessExpression)
        {
            FlattenAndPrintNodes(elementAccessExpression.Expression, printedNodes, context);
            printedNodes.Add(
                new PrintedNode(
                    elementAccessExpression,
                    Node.Print(elementAccessExpression.ArgumentList, context)
                )
            );
        }
        else if (expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            FlattenAndPrintNodes(memberAccessExpressionSyntax.Expression, printedNodes, context);
            printedNodes.Add(
                new PrintedNode(
                    memberAccessExpressionSyntax,
                    Doc.Concat(
                        Token.Print(memberAccessExpressionSyntax.OperatorToken, context),
                        Node.Print(memberAccessExpressionSyntax.Name, context)
                    )
                )
            );
        }
        else if (expression is ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
        {
            FlattenAndPrintNodes(
                conditionalAccessExpressionSyntax.Expression,
                printedNodes,
                context
            );
            printedNodes.Add(
                new PrintedNode(
                    conditionalAccessExpressionSyntax,
                    Token.Print(conditionalAccessExpressionSyntax.OperatorToken, context)
                )
            );
            FlattenAndPrintNodes(
                conditionalAccessExpressionSyntax.WhenNotNull,
                printedNodes,
                context
            );
        }
        else if (
            expression is PostfixUnaryExpressionSyntax
            {
                Operand: InvocationExpressionSyntax or MemberAccessExpressionSyntax
            } postfixUnaryExpression
        )
        {
            FlattenAndPrintNodes(postfixUnaryExpression.Operand, printedNodes, context);
            printedNodes.Add(
                new PrintedNode(
                    postfixUnaryExpression,
                    Token.Print(postfixUnaryExpression.OperatorToken, context)
                )
            );
        }
        else
        {
            printedNodes.Add(new PrintedNode(expression, Node.Print(expression, context)));
        }
    }

    private static List<List<PrintedNode>> GroupPrintedNodesOnLines(List<PrintedNode> printedNodes)
    {
        // We want to group the printed nodes in the following manner
        //
        //   a?.b.c!.d

        // so that we can print it like this if it breaks
        //   a
        //     ?.b
        //     .c!
        //     .d

        var groups = new List<List<PrintedNode>>();

        var currentGroup = new List<PrintedNode> { printedNodes[0] };
        groups.Add(currentGroup);

        for (var index = 1; index < printedNodes.Count; index++)
        {
            if (printedNodes[index].Node is ConditionalAccessExpressionSyntax)
            {
                currentGroup = [];
                groups.Add(currentGroup);
            }
            else if (
                printedNodes[index].Node
                    is MemberAccessExpressionSyntax
                        or MemberBindingExpressionSyntax
                        or IdentifierNameSyntax
                && printedNodes[index + -1].Node is not ConditionalAccessExpressionSyntax
            )
            {
                currentGroup = [];
                groups.Add(currentGroup);
            }

            currentGroup.Add(printedNodes[index]);
        }

        return groups;
    }

    private static List<List<PrintedNode>> GroupPrintedNodesPrettierStyle(
        List<PrintedNode> printedNodes
    )
    {
        // We want to group the printed nodes in the following manner
        //
        //   a().b.c().d().e
        // will be grouped as
        //   [
        //     [Identifier, InvocationExpression],
        //     [MemberAccessExpression], [MemberAccessExpression, InvocationExpression],
        //     [MemberAccessExpression, InvocationExpression],
        //     [MemberAccessExpression],
        //   ]

        // so that we can print it as
        //   a()
        //     .b.c()
        //     .d()
        //     .e

        // TODO #451 this whole thing could possibly just turn into a big loop
        // based on the current node, and the next/previous node, decide when to create new groups.
        // certain nodes need to stay in the current group, other nodes indicate that a new group needs to be created.
        var groups = new List<List<PrintedNode>>();
        var currentGroup = new List<PrintedNode> { printedNodes[0] };
        var index = 1;
        for (; index < printedNodes.Count; index++)
        {
            if (printedNodes[index].Node is InvocationExpressionSyntax)
            {
                currentGroup.Add(printedNodes[index]);
            }
            else
            {
                break;
            }
        }

        if (
            printedNodes[0].Node is not (InvocationExpressionSyntax or PostfixUnaryExpressionSyntax)
            && index < printedNodes.Count
            && printedNodes[index].Node
                is ElementAccessExpressionSyntax
                    or PostfixUnaryExpressionSyntax
        )
        {
            currentGroup.Add(printedNodes[index]);
            index++;
        }

        groups.Add(currentGroup);
        currentGroup = [];

        var hasSeenNodeThatRequiresBreak = false;
        for (; index < printedNodes.Count; index++)
        {
            if (
                hasSeenNodeThatRequiresBreak
                && printedNodes[index].Node
                    is MemberAccessExpressionSyntax
                        or ConditionalAccessExpressionSyntax
            )
            {
                groups.Add(currentGroup);
                currentGroup = [];
                hasSeenNodeThatRequiresBreak = false;
            }

            if (
                printedNodes[index].Node
                is InvocationExpressionSyntax
                    or ElementAccessExpressionSyntax
            )
            {
                hasSeenNodeThatRequiresBreak = true;
            }
            currentGroup.Add(printedNodes[index]);
        }

        if (currentGroup.Count != 0)
        {
            groups.Add(currentGroup);
        }

        return groups;
    }

    [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
    private static Doc[] SelectManyDocsToArray(List<List<PrintedNode>> groups)
    {
        var arrayLength = 0;
        foreach (var group in groups)
        {
            arrayLength += group.Count;
        }

        var outputArray = new Doc[arrayLength];

        var pos = 0;
        foreach (var group in groups)
        {
            foreach (var node in group)
            {
                outputArray[pos] = node.Doc;
                pos++;
            }
        }

        return outputArray;
    }

    private static Doc PrintIndentedGroup(List<List<PrintedNode>> groups)
    {
        if (groups.Count == 0)
        {
            return Doc.Null;
        }

        var result = new DocListBuilder(groups.Count * 2);

        for (var index = 0; index < groups.Count; index++)
        {
            Doc GetPossibleContents()
            {
                if (index >= groups.Count)
                {
                    return Doc.Null;
                }
                var nextGroup = groups[index];
                if (
                    nextGroup[0].Node
                    is not MemberAccessExpressionSyntax
                    {
                        Name: IdentifierNameSyntax { Identifier.Text: "ThenInclude" }
                    }
                )
                {
                    return Doc.Null;
                }

                index++;
                return Doc.Indent(
                    Doc.HardLine,
                    Doc.Group(nextGroup.Select(p => p.Doc).ToArray()),
                    GetPossibleContents()
                );
            }

            var possibleContents = GetPossibleContents();
            if (possibleContents != Doc.Null)
            {
                index--;
                result.Add(possibleContents);
            }
            else
            {
                var group = groups[index];
                result.Add(Doc.HardLine);
                result.Add(Doc.Group(group.Select(p => p.Doc).ToArray()));
            }
        }

        return Doc.Indent(Doc.Group(result.ToArray()));
    }

    // Fork (csharpier-editorconfig): a chain that forms a statement - an expression statement,
    // an if/while/foreach/switch condition, a return - keeps its first link on the root line.
    // A chain whose value is assigned or passed on has an expression node as its parent and
    // starts every link on its own line, so the '=' stays readable. An expression bodied member
    // is handled in ArrowExpressionClause instead: the root stays on the "=>" line.
    private static bool IsStatementChain(SyntaxNode? parent)
    {
        return parent is StatementSyntax
            || (
                parent is AwaitExpressionSyntax awaitExpression
                && IsStatementChain(awaitExpression.Parent)
            );
    }

    // There are cases where merging the first two groups looks better
    // For example
    /*
        // without merging we get this
        this
            .CallMethod()
            .CallMethod();

        // merging gives us this
        this.CallMethod()
            .CallMethod();
     */
    private static bool ShouldMergeFirstTwoGroups(
        List<List<PrintedNode>> groups,
        SyntaxNode? parent
    )
    {
        if (groups.Count < 2 || groups[0].Count != 1)
        {
            return false;
        }

        var firstNode = groups[0][0].Node;

        if (
            firstNode
            is not (
                IdentifierNameSyntax { Identifier.Text.Length: <= 4 }
                or ThisExpressionSyntax
                or PredefinedTypeSyntax
                or BaseExpressionSyntax
            )
        )
        {
            return false;
        }

        // TODO maybe some things to fix in here
        // https://github.com/belav/csharpier-repos/pull/100/files
        if (
            groups[1].Count == 1
            || parent
                is SimpleLambdaExpressionSyntax
                    or ArgumentSyntax
                    or BinaryExpressionSyntax
                    or ExpressionStatementSyntax
            || groups[1].Skip(1).First().Node
                is InvocationExpressionSyntax
                    or ElementAccessExpressionSyntax
                    or PostfixUnaryExpressionSyntax
        )
        {
            return true;
        }

        return false;
    }
}
