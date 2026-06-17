// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

internal static class CSharpUseLabeledJumpStatementsHelpers
{
    public static bool IsLoop(SyntaxNode node)
        => node.Kind() is SyntaxKind.WhileStatement
            or SyntaxKind.DoStatement
            or SyntaxKind.ForStatement
            or SyntaxKind.ForEachStatement
            or SyntaxKind.ForEachVariableStatement;

    /// <summary>
    /// Determines whether <paramref name="gotoStatement"/> is a <c>goto</c> that emulates a (possibly multi-level)
    /// <c>break</c> out of an enclosing loop: it targets a label placed on the statement immediately following that
    /// loop, every reference to that label is such a <c>goto</c> nested in the loop, and no jump would have to cross a
    /// <c>finally</c> to reach the loop. On success, <paramref name="loop"/> is the loop to label, <paramref
    /// name="labelDeclaration"/> is the (now redundant) label after the loop, and <paramref name="gotos"/> is every
    /// jump to rewrite.
    /// </summary>
    public static bool TryGetGotoBreakPattern(
        GotoStatementSyntax gotoStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out StatementSyntax loop,
        out LabeledStatementSyntax labelDeclaration,
        out ImmutableArray<GotoStatementSyntax> gotos)
    {
        loop = null!;
        labelDeclaration = null!;
        gotos = default;

        if (!IsPlainGoto(gotoStatement))
            return false;

        if (semanticModel.GetSymbolInfo(gotoStatement.Expression, cancellationToken).Symbol is not ILabelSymbol label)
            return false;

        if (label.DeclaringSyntaxReferences is not [var reference] ||
            reference.GetSyntax(cancellationToken) is not LabeledStatementSyntax decl ||
            decl.Parent is not BlockSyntax block)
        {
            return false;
        }

        // The label must sit on the statement immediately following a loop.  That makes "jump to the label" the same
        // as "break that loop" (the loop's end point).
        var index = block.Statements.IndexOf(decl);
        if (index <= 0)
            return false;

        var precedingLoop = block.Statements[index - 1];
        if (!IsLoop(precedingLoop))
            return false;

        // Every reference to the label must be a 'goto' nested in the loop (so the label can be moved onto the loop
        // and each jump converted), and reaching the loop's end must not cross a 'finally'.
        using var _ = ArrayBuilder<GotoStatementSyntax>.GetInstance(out var builder);
        foreach (var candidate in block.DescendantNodes().OfType<GotoStatementSyntax>())
        {
            if (!IsPlainGoto(candidate) ||
                !Equals(semanticModel.GetSymbolInfo(candidate.Expression, cancellationToken).Symbol, label))
            {
                continue;
            }

            if (!precedingLoop.Contains(candidate) || CrossesFinally(candidate, precedingLoop))
                return false;

            builder.Add(candidate);
        }

        if (builder.Count == 0)
            return false;

        loop = precedingLoop;
        labelDeclaration = decl;
        gotos = builder.ToImmutable();
        return true;
    }

    private static bool IsPlainGoto(GotoStatementSyntax gotoStatement)
        => gotoStatement.Kind() == SyntaxKind.GotoStatement && gotoStatement.Expression is IdentifierNameSyntax;

    private static bool CrossesFinally(SyntaxNode jump, SyntaxNode target)
    {
        for (var current = jump; current != null && current != target; current = current.Parent)
        {
            if (current is FinallyClauseSyntax)
                return true;
        }

        return false;
    }
}
