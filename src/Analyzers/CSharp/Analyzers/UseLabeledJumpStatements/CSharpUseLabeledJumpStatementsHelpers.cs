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

    public static StatementSyntax? GetLoopBody(SyntaxNode loop)
        => loop switch
        {
            WhileStatementSyntax whileStatement => whileStatement.Statement,
            DoStatementSyntax doStatement => doStatement.Statement,
            ForStatementSyntax forStatement => forStatement.Statement,
            CommonForEachStatementSyntax forEachStatement => forEachStatement.Statement,
            _ => null,
        };

    /// <summary>
    /// Determines whether <paramref name="gotoStatement"/> is a <c>goto</c> that emulates a multi-level <c>break</c>:
    /// it targets a label placed on the statement immediately following an enclosing loop, every reference to that
    /// label is such a <c>goto</c> nested in the loop, at least one of them is genuinely nested inside an inner
    /// loop/switch (so a plain <c>break;</c> would not suffice), and reaching the loop does not cross a <c>finally</c>.
    /// On success, <paramref name="loop"/> is the loop to label, <paramref name="labelDeclaration"/> is the (now
    /// redundant) label after the loop, and <paramref name="gotos"/> is every jump to rewrite.
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

        if (!TryResolveLabel(gotoStatement, semanticModel, cancellationToken, out var label, out var declaration) ||
            declaration.Parent is not BlockSyntax block)
        {
            return false;
        }

        // The label must sit on the statement immediately following a loop.  That makes "jump to the label" the same
        // as "break that loop" (the loop's end point).
        var index = block.Statements.IndexOf(declaration);
        if (index <= 0)
            return false;

        var precedingLoop = block.Statements[index - 1];
        if (!IsLoop(precedingLoop))
            return false;

        if (!TryCollectMultiLevelJumps(precedingLoop, label, semanticModel, cancellationToken, includeSwitch: true, out gotos))
            return false;

        loop = precedingLoop;
        labelDeclaration = declaration;
        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="gotoStatement"/> is a <c>goto</c> that emulates a multi-level
    /// <c>continue</c>: it targets a label that is the last statement of an enclosing loop's body (so jumping to it is
    /// the same as continuing that loop), every reference is such a <c>goto</c> nested in the loop, at least one is
    /// nested inside an inner loop (so a plain <c>continue;</c> would not suffice), and no jump crosses a
    /// <c>finally</c>. On success, <paramref name="loop"/> is the loop to label, <paramref name="labelDeclaration"/> is
    /// the (now redundant) trailing label, and <paramref name="gotos"/> is every jump to rewrite.
    /// </summary>
    public static bool TryGetGotoContinuePattern(
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

        if (!TryResolveLabel(gotoStatement, semanticModel, cancellationToken, out var label, out var declaration) ||
            declaration.Parent is not BlockSyntax body ||
            body.Statements.LastOrDefault() != declaration ||
            declaration.Statement is not EmptyStatementSyntax)
        {
            // The label must be an empty statement at the very end of the body.  Otherwise jumping to it would run
            // some trailing work that a 'continue' would skip, so the rewrite would not be equivalent.
            return false;
        }

        // The label must be the final statement of a loop's body block, so jumping to it falls through to the loop's
        // next iteration -- exactly what 'continue' does.
        if (body.Parent is not StatementSyntax candidateLoop || !IsLoop(candidateLoop) || GetLoopBody(candidateLoop) != body)
            return false;

        if (!TryCollectMultiLevelJumps(candidateLoop, label, semanticModel, cancellationToken, includeSwitch: false, out gotos))
            return false;

        loop = candidateLoop;
        labelDeclaration = declaration;
        return true;
    }

    private static bool TryResolveLabel(
        GotoStatementSyntax gotoStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out ILabelSymbol label,
        out LabeledStatementSyntax declaration)
    {
        label = null!;
        declaration = null!;

        if (!IsPlainGoto(gotoStatement))
            return false;

        if (semanticModel.GetSymbolInfo(gotoStatement.Expression, cancellationToken).Symbol is not ILabelSymbol labelSymbol)
            return false;

        if (labelSymbol.DeclaringSyntaxReferences is not [var reference] ||
            reference.GetSyntax(cancellationToken) is not LabeledStatementSyntax labelDeclaration)
        {
            return false;
        }

        label = labelSymbol;
        declaration = labelDeclaration;
        return true;
    }

    private static bool TryCollectMultiLevelJumps(
        StatementSyntax loop,
        ILabelSymbol label,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        bool includeSwitch,
        out ImmutableArray<GotoStatementSyntax> gotos)
    {
        gotos = default;

        // Every reference to the label must be a 'goto' nested in the loop (so the label can be moved onto the loop
        // and each jump converted), reaching the loop must not cross a 'finally', and at least one jump must be nested
        // deeply enough that a plain (unlabeled) 'break'/'continue' would not reach this loop.
        using var _ = ArrayBuilder<GotoStatementSyntax>.GetInstance(out var builder);
        var anyRequiresLabel = false;
        foreach (var candidate in loop.DescendantNodes().OfType<GotoStatementSyntax>())
        {
            if (!IsPlainGoto(candidate) ||
                !Equals(semanticModel.GetSymbolInfo(candidate.Expression, cancellationToken).Symbol, label))
            {
                continue;
            }

            if (CrossesFinally(candidate, loop))
                return false;

            builder.Add(candidate);
            anyRequiresLabel |= CrossesInnerLoopOrSwitch(candidate, loop, includeSwitch);
        }

        if (builder.Count == 0 || !anyRequiresLabel)
            return false;

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

    private static bool CrossesInnerLoopOrSwitch(SyntaxNode jump, SyntaxNode target, bool includeSwitch)
    {
        for (var current = jump.Parent; current != null && current != target; current = current.Parent)
        {
            if (IsLoop(current) || (includeSwitch && current is SwitchStatementSyntax))
                return true;
        }

        return false;
    }
}
