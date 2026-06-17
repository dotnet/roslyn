// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

internal static partial class CSharpUseLabeledJumpStatementsHelpers
{
    /// <summary>
    /// Determines whether <paramref name="gotoStatement"/> is a <c>goto</c> that emulates a multi-level <c>break</c>:
    /// it targets a label placed on the statement immediately following an enclosing loop, every reference to that
    /// label is such a <c>goto</c> nested in the construct, at least one of them is genuinely nested inside an inner
    /// loop/switch (so a plain <c>break;</c> would not suffice), and reaching the construct does not cross a
    /// <c>finally</c>. The construct may be a loop or a <c>switch</c> (both are valid <c>break</c> targets). On
    /// success, <paramref name="breakTarget"/> is the construct to label, <paramref name="labelDeclaration"/> is the
    /// (now redundant) label after it, and <paramref name="gotos"/> is every jump to rewrite.
    /// </summary>
    public static bool TryGetGotoBreakPattern(
        GotoStatementSyntax gotoStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out StatementSyntax? breakTarget,
        [NotNullWhen(true)] out LabeledStatementSyntax? labelDeclaration,
        out ImmutableArray<GotoStatementSyntax> gotos)
    {
        breakTarget = null;
        labelDeclaration = null;
        gotos = default;

        if (!TryResolveLabel(gotoStatement, semanticModel, cancellationToken, out var label, out var declaration))
            return false;

        // GetPreviousStatement handles the block, switch-section, and top-level (global statement) cases.
        if (declaration.GetPreviousStatement() is not { } precedingConstruct || !precedingConstruct.IsBreakableConstruct())
            return false;

        if (!TryCollectMultiLevelJumps(precedingConstruct, label, semanticModel, cancellationToken, includeSwitch: true, out gotos))
            return false;

        breakTarget = precedingConstruct;
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
        [NotNullWhen(true)] out StatementSyntax? loop,
        [NotNullWhen(true)] out LabeledStatementSyntax? labelDeclaration,
        out ImmutableArray<GotoStatementSyntax> gotos)
    {
        loop = null;
        labelDeclaration = null;
        gotos = default;

        if (!TryResolveLabel(gotoStatement, semanticModel, cancellationToken, out var label, out var declaration) ||
            declaration.Parent is not BlockSyntax body ||
            body.Statements.LastOrDefault() != declaration ||
            declaration.Statement is not EmptyStatementSyntax)
        {
            // Requiring an empty label at the very end of the body keeps the rewrite equivalent: jumping there would
            // otherwise run trailing work that a 'continue' would skip.
            return false;
        }

        if (body.Parent is not StatementSyntax candidateLoop || !candidateLoop.IsContinuableConstruct() || candidateLoop.GetEmbeddedStatement() != body)
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
        [NotNullWhen(true)] out ILabelSymbol? label,
        [NotNullWhen(true)] out LabeledStatementSyntax? declaration)
    {
        label = null;
        declaration = null;

        if (!IsPlainGoto(gotoStatement, out var labelReference))
            return false;

        if (semanticModel.GetSymbolInfo(labelReference, cancellationToken).Symbol is not ILabelSymbol labelSymbol)
            return false;

        if (labelSymbol.DeclaringSyntaxReferences is not [var reference] ||
            reference.GetSyntax(cancellationToken) is not LabeledStatementSyntax labeledStatement)
        {
            return false;
        }

        label = labelSymbol;
        declaration = labeledStatement;
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
            if (!IsPlainGoto(candidate, out var candidateLabel) ||
                !Equals(semanticModel.GetSymbolInfo(candidateLabel, cancellationToken).Symbol, label))
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

    private static bool IsPlainGoto(GotoStatementSyntax gotoStatement, [NotNullWhen(true)] out IdentifierNameSyntax? label)
    {
        if (gotoStatement.Kind() == SyntaxKind.GotoStatement && gotoStatement.Expression is IdentifierNameSyntax identifier)
        {
            label = identifier;
            return true;
        }

        label = null;
        return false;
    }

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
            if (includeSwitch ? current.IsBreakableConstruct() : current.IsContinuableConstruct())
                return true;
        }

        return false;
    }
}
