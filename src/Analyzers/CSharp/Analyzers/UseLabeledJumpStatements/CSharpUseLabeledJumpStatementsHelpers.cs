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
    /// A <c>goto</c> emulating a multi-level <c>break</c> out of an enclosing loop or <c>switch</c>:
    /// <code>
    /// while (...)                  // breakTarget
    /// {
    ///     while (...)
    ///         goto found;          // gotos
    /// }
    /// found: ...                   // labelDeclaration
    /// </code>
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
        var precedingConstruct = declaration.GetPreviousStatement();

        // A prior fix may have already labeled this loop ('outer: while (...)').  Unwrap to the loop/switch itself so
        // the construct is still recognized (the existing label is reused later, in the fix).
        if (precedingConstruct is LabeledStatementSyntax labeledConstruct)
            precedingConstruct = labeledConstruct.Statement;

        if (!precedingConstruct.IsBreakableConstruct())
            return false;

        if (!TryCollectMultiLevelJumps(precedingConstruct, label, semanticModel, cancellationToken, includeSwitch: true, out gotos))
            return false;

        breakTarget = precedingConstruct;
        labelDeclaration = declaration;
        return true;
    }

    /// <summary>
    /// A <c>goto</c> emulating a multi-level <c>continue</c> of an enclosing loop:
    /// <code>
    /// while (...)                  // loop
    /// {
    ///     while (...)
    ///         goto next;           // gotos
    ///     next: ;                  // labelDeclaration
    /// }
    /// </code>
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

        // Requiring an empty label as the very last statement of a loop body keeps the rewrite equivalent: jumping
        // there would otherwise run trailing work that a 'continue' would skip.
        if (TryResolveLabel(gotoStatement, semanticModel, cancellationToken, out var label, out var declaration) &&
            declaration is
            {
                Parent: BlockSyntax { Parent: StatementSyntax candidateLoop, Statements: [.., var lastStatement] } body,
                Statement: EmptyStatementSyntax
            } &&
            lastStatement == declaration &&
            candidateLoop.IsContinuableConstruct() &&
            TryCollectMultiLevelJumps(candidateLoop, label, semanticModel, cancellationToken, includeSwitch: false, out gotos))
        {
            loop = candidateLoop;
            labelDeclaration = declaration;
            return true;
        }

        return false;
    }

    private static bool TryResolveLabel(
        GotoStatementSyntax gotoStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ILabelSymbol? label,
        [NotNullWhen(true)] out LabeledStatementSyntax? labeledStatement)
    {
        label = null;
        labeledStatement = null;

        if (!IsPlainGoto(gotoStatement, out var labelReference))
            return false;

        label = semanticModel.GetSymbolInfo(labelReference, cancellationToken).Symbol as ILabelSymbol;
        labeledStatement = label?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as LabeledStatementSyntax;

        return label != null && labeledStatement != null;
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
