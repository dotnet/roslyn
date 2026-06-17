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

// Reads BreakStatementSyntax.Name/ContinueStatementSyntax.Name, which are preview (labeled break/continue) APIs.
#pragma warning disable RSEXPERIMENTAL006

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

internal static partial class CSharpUseLabeledJumpStatementsHelpers
{
    /// <summary>
    /// A <c>bool</c> flag emulating a multi-level break/continue.  The flag is set and breaks the innermost loop, then a
    /// chain of <c>if (flag) break;</c> guards propagates the exit outward (one per intervening loop) up to a final
    /// <c>if (flag) break/continue;</c> that decides the action on the loop we relabel:
    /// <code>
    /// bool flag = false;                            // LocalDeclarationStatement
    /// while (...)                                   // LoopStatement (the final guard's target)
    /// {
    ///     while (...)
    ///     {
    ///         while (...)
    ///             if (...) { flag = true; break; }  // AssignmentAndBreakSites (break the innermost loop)
    ///         if (flag) break;                      // GuardStatements (intermediate; always 'break')
    ///     }
    ///     if (flag) break;                          // GuardStatements (final; 'break' or 'continue')
    /// }
    /// </code>
    /// </summary>
    public static bool TryGetFlagPattern(
        LocalDeclarationStatementSyntax declaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out FlagJumpPattern? pattern)
    {
        pattern = null;

        if (declaration is not
            {
                Declaration.Variables: [{ Initializer.Value: LiteralExpressionSyntax(SyntaxKind.FalseLiteralExpression) } declarator],
                Parent: BlockSyntax enclosingBlock,
            })
        {
            return false;
        }

        if (semanticModel.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol { Type.SpecialType: SpecialType.System_Boolean } flag)
            return false;

        using var _ = ArrayBuilder<(ExpressionStatementSyntax Assignment, BreakStatementSyntax Break)>.GetInstance(out var sites);
        using var _1 = PooledHashSet<IfStatementSyntax>.GetInstance(out var guards);

        // Classify every reference to the flag.  It may only be a 'flag = true;' (immediately before a 'break;') or an
        // 'if (flag) break/continue;' guard.  Anything else means the flag is doing more than propagating a jump.
        foreach (var name in enclosingBlock.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (name.Identifier.ValueText != flag.Name ||
                !Equals(semanticModel.GetSymbolInfo(name, cancellationToken).Symbol, flag))
            {
                continue;
            }

            if (TryGetAssignmentToTrueSite(name, out var assignment, out var innerBreak))
                sites.Add((assignment, innerBreak));
            else if (TryGetGuard(name, out var ifStatement))
                guards.Add(ifStatement);
            else
                return false;
        }

        if (sites.Count == 0 || guards.Count == 0)
            return false;

        // Every site must break the same innermost loop with a plain (unlabeled) 'break;'.
        if (GetNearestBreakOrContinueTarget(sites[0].Break, isBreak: true) is not StatementSyntax innerLoop ||
            !innerLoop.IsContinuableConstruct())
        {
            return false;
        }

        foreach (var (_, innerBreak) in sites)
        {
            if (innerBreak.Name != null || GetNearestBreakOrContinueTarget(innerBreak, isBreak: true) != innerLoop)
                return false;
        }

        using var _2 = ArrayBuilder<IfStatementSyntax>.GetInstance(out var orderedGuards);
        if (!TryWalkGuardChain(innerLoop, guards, orderedGuards, out var targetLoop, out var isBreak))
            return false;

        // The declaration must live outside the loop we relabel (the fix removes it separately), and no site may cross
        // a 'finally' on its way out to that loop.
        if (targetLoop.Contains(declaration))
            return false;

        foreach (var (_, innerBreak) in sites)
        {
            if (CrossesFinally(innerBreak, targetLoop))
                return false;
        }

        pattern = new FlagJumpPattern
        {
            LocalDeclarationStatement = declaration,
            LoopStatement = targetLoop,
            GuardStatements = orderedGuards.ToImmutable(),
            AssignmentAndBreakSites = sites.ToImmutable(),
            IsBreak = isBreak,
        };
        return true;
    }

    /// <summary>
    /// Walks outward from <paramref name="innerLoop"/> (the loop the sites break), consuming the chain of
    /// <c>if (flag) break;</c> guards — each immediately following the loop it exits — up to a final
    /// <c>if (flag) break/continue;</c>.  The loop that final guard targets (<paramref name="targetLoop"/>) is the one
    /// to relabel.  Fails unless every collected guard belongs to the chain.
    /// </summary>
    private static bool TryWalkGuardChain(
        StatementSyntax innerLoop,
        PooledHashSet<IfStatementSyntax> guards,
        ArrayBuilder<IfStatementSyntax> orderedGuards,
        [NotNullWhen(true)] out StatementSyntax? targetLoop,
        out bool isBreak)
    {
        targetLoop = null;
        isBreak = false;

        for (var current = innerLoop; current != null;)
        {
            // The statement following 'current' in its block (accounting for a label a prior fix may have added).
            var statementInBlock = current.Parent is LabeledStatementSyntax labeled ? labeled : current;
            if (statementInBlock.Parent is not BlockSyntax { Parent: StatementSyntax containingLoop } containingBlock ||
                !containingLoop.IsContinuableConstruct())
            {
                break;
            }

            var nextIndex = containingBlock.Statements.IndexOf(statementInBlock) + 1;
            if (nextIndex == 0 ||
                nextIndex >= containingBlock.Statements.Count ||
                containingBlock.Statements[nextIndex] is not IfStatementSyntax guard ||
                !guards.Contains(guard))
            {
                break;
            }

            var guardJump = Unwrap(guard.Statement);
            if (!IsUnlabeledLoopJump(guardJump))
                return false;

            orderedGuards.Add(guard);
            targetLoop = containingLoop;
            isBreak = guardJump is BreakStatementSyntax;

            // A 'continue' cannot propagate further, so it is necessarily the final guard.
            if (!isBreak)
                break;

            current = containingLoop;
        }

        // Every collected guard must belong to the chain; a stray one means the flag is used for something else.
        return targetLoop != null && orderedGuards.Count == guards.Count;
    }

    /// <summary>
    /// The flag pattern (see <see cref="TryGetFlagPattern"/>) entered from its inner jump:
    /// <code>
    /// flag = true;
    /// break;          // breakStatement
    /// </code>
    /// </summary>
    public static bool TryGetFlagPatternFromInnerBreak(
        BreakStatementSyntax breakStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out FlagJumpPattern? pattern)
    {
        pattern = null;

        if (breakStatement is not { Name: null, Parent: BlockSyntax block })
            return false;

        var index = block.Statements.IndexOf(breakStatement);
        if (index <= 0 ||
            block.Statements[index - 1] is not ExpressionStatementSyntax
            {
                Expression: AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression)
                {
                    Left: IdentifierNameSyntax flagName,
                    Right: LiteralExpressionSyntax(SyntaxKind.TrueLiteralExpression),
                }
            })
        {
            return false;
        }

        if (semanticModel.GetSymbolInfo(flagName, cancellationToken).Symbol is not ILocalSymbol flag ||
            flag.DeclaringSyntaxReferences is not [var reference] ||
            reference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax
            {
                Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax declaration }
            })
        {
            return false;
        }

        if (!TryGetFlagPattern(declaration, semanticModel, cancellationToken, out pattern) ||
            !pattern.AssignmentAndBreakSites.Any(site => site.Break == breakStatement))
        {
            pattern = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// A flag reference <paramref name="name"/> used as the assignment in a site that sets the flag and breaks the
    /// inner loop:
    /// <code>
    /// flag = true;    // assignment
    /// break;          // innerBreak (immediately follows)
    /// </code>
    /// </summary>
    private static bool TryGetAssignmentToTrueSite(
        IdentifierNameSyntax name,
        [NotNullWhen(true)] out ExpressionStatementSyntax? assignment,
        [NotNullWhen(true)] out BreakStatementSyntax? innerBreak)
    {
        assignment = null;
        innerBreak = null;

        // 'name' can only be the left side: the right side is required to be a 'true' literal.
        if (name.Parent is not AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression)
            {
                Right: LiteralExpressionSyntax(SyntaxKind.TrueLiteralExpression),
                Parent: ExpressionStatementSyntax { Parent: BlockSyntax block } assignmentStatement,
            })
        {
            return false;
        }

        // The assignment must be immediately followed by a 'break;'.
        var index = block.Statements.IndexOf(assignmentStatement);
        if (index < 0 || index + 1 >= block.Statements.Count || block.Statements[index + 1] is not BreakStatementSyntax breakStatement)
            return false;

        assignment = assignmentStatement;
        innerBreak = breakStatement;
        return true;
    }

    private static bool TryGetGuard(IdentifierNameSyntax name, [NotNullWhen(true)] out IfStatementSyntax? ifStatement)
    {
        ifStatement = null;

        if (name.Parent is not IfStatementSyntax { Else: null } candidate || candidate.Condition != name)
            return false;

        ifStatement = candidate;
        return true;
    }

    private static StatementSyntax Unwrap(StatementSyntax statement)
        => statement is BlockSyntax { Statements: [var single] } ? single : statement;

    private static bool IsUnlabeledLoopJump(StatementSyntax statement)
        => statement is BreakStatementSyntax { Name: null } or ContinueStatementSyntax { Name: null };

    private static SyntaxNode? GetNearestBreakOrContinueTarget(SyntaxNode node, bool isBreak)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (isBreak ? current.IsBreakableConstruct() : current.IsContinuableConstruct())
                return current;

            if (current is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax)
                return null;
        }

        return null;
    }
}
