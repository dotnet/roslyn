// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

/// <summary>
/// A recognized <c>bool</c>-flag emulation of a multi-level <c>break</c>/<c>continue</c>.
/// </summary>
internal sealed class FlagJumpPattern
{
    /// <summary>The <c>bool flag = false;</c> declaration to delete.</summary>
    public required LocalDeclarationStatementSyntax Declaration { get; init; }

    /// <summary>The outer loop to label and break/continue.</summary>
    public required StatementSyntax Loop { get; init; }

    /// <summary>The <c>if (flag) break;</c>/<c>if (flag) continue;</c> guard to delete.</summary>
    public required IfStatementSyntax Guard { get; init; }

    /// <summary>The inner <c>flag = true; break;</c> sites; each break becomes the labeled jump.</summary>
    public required ImmutableArray<(ExpressionStatementSyntax Assignment, BreakStatementSyntax Break)> Sites { get; init; }

    /// <summary>Whether the guard is a <c>break</c> (otherwise a <c>continue</c>).</summary>
    public required bool IsBreak { get; init; }
}

internal static partial class CSharpUseLabeledJumpStatementsHelpers
{
    /// <summary>
    /// Recognizes the conservative shape <c>bool flag = false; ... inner-loop { ...; flag = true; break; } ... if
    /// (flag) break/continue;</c> where the <c>flag</c> exists only to propagate a jump out of (or to continue) an
    /// outer loop. The flag's <em>only</em> uses must be the <c>flag = true;</c> assignments (each immediately
    /// followed by a <c>break;</c> of the inner loop) and the single <c>if (flag)</c> guard sitting immediately after
    /// that inner loop in the outer loop's body. Anything else (resets, extra reads, ...) bails.
    /// </summary>
    public static bool TryGetFlagPattern(
        LocalDeclarationStatementSyntax declaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out FlagJumpPattern? pattern)
    {
        pattern = null;

        if (declaration.Declaration.Variables is not [{ Initializer.Value: LiteralExpressionSyntax initializer } declarator] ||
            initializer.Kind() != SyntaxKind.FalseLiteralExpression ||
            declaration.Parent is not BlockSyntax enclosingBlock)
        {
            return false;
        }

        if (semanticModel.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol { Type.SpecialType: SpecialType.System_Boolean } flag)
            return false;

        using var _ = ArrayBuilder<(ExpressionStatementSyntax, BreakStatementSyntax)>.GetInstance(out var sites);
        IfStatementSyntax? guard = null;

        // Classify every reference to the flag.  It may only be a 'flag = true;' (immediately before a 'break;') or
        // the single 'if (flag) break/continue;' guard.  Anything else means the flag is doing more than propagating a
        // jump, so we bail.
        foreach (var name in enclosingBlock.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (name.Identifier.ValueText != flag.Name ||
                !Equals(semanticModel.GetSymbolInfo(name, cancellationToken).Symbol, flag))
            {
                continue;
            }

            if (TryGetAssignmentToTrueSite(name, out var assignment, out var innerBreak))
            {
                sites.Add((assignment, innerBreak));
            }
            else if (TryGetGuard(name, out var ifStatement))
            {
                if (guard != null)
                    return false;

                guard = ifStatement;
            }
            else
            {
                return false;
            }
        }

        if (guard is null || sites.Count == 0)
            return false;

        var guardJump = Unwrap(guard.Statement);
        var isBreak = guardJump is BreakStatementSyntax;

        if (!IsUnlabeledLoopJump(guardJump))
            return false;

        if (guard.Parent is not BlockSyntax guardBlock ||
            guardBlock.Parent is not StatementSyntax outerLoop ||
            !outerLoop.IsContinuableConstruct() ||
            outerLoop.GetEmbeddedStatement() != guardBlock)
        {
            return false;
        }

        var guardIndex = guardBlock.Statements.IndexOf(guard);
        if (guardIndex <= 0 || guardBlock.Statements[guardIndex - 1] is not StatementSyntax innerLoop || !innerLoop.IsContinuableConstruct())
            return false;

        // Every 'flag = true; break;' site must break that inner loop and not cross a 'finally' on the way to the
        // outer loop.
        foreach (var (_, innerBreak) in sites)
        {
            if (innerBreak.Name != null ||
                !innerLoop.Contains(innerBreak) ||
                GetNearestBreakOrContinueTarget(innerBreak, isBreak: true) != innerLoop ||
                CrossesFinally(innerBreak, outerLoop))
            {
                return false;
            }
        }

        pattern = new FlagJumpPattern
        {
            Declaration = declaration,
            Loop = outerLoop,
            Guard = guard,
            Sites = sites.ToImmutable(),
            IsBreak = isBreak,
        };
        return true;
    }

    /// <summary>
    /// Recognizes the flag pattern starting from an inner <c>break;</c> (the actionable jump): it must be immediately
    /// preceded by <c>flag = true;</c>, where <c>flag</c> is a local satisfying <see cref="TryGetFlagPattern"/>.
    /// </summary>
    public static bool TryGetFlagPatternFromInnerBreak(
        BreakStatementSyntax breakStatement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out FlagJumpPattern? pattern)
    {
        pattern = null;

        if (breakStatement.Name != null || breakStatement.Parent is not BlockSyntax block)
            return false;

        var index = block.Statements.IndexOf(breakStatement);
        if (index <= 0 ||
            block.Statements[index - 1] is not ExpressionStatementSyntax
            {
                Expression: AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                    Left: IdentifierNameSyntax flagName,
                    Right: LiteralExpressionSyntax rhs,
                }
            } ||
            rhs.Kind() != SyntaxKind.TrueLiteralExpression)
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
            !pattern.Sites.Any(site => site.Break == breakStatement))
        {
            pattern = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Synthesizes a label name for <paramref name="loop"/> (<c>loop_i</c>/<c>loop_x</c> from a for/foreach variable,
    /// else <c>outer</c>), uniquified against the labels already declared in the enclosing member.
    /// </summary>
    public static string GenerateLabelName(StatementSyntax loop)
    {
        var baseName = loop switch
        {
            ForStatementSyntax { Declaration.Variables: [var variable, ..] } => "loop_" + variable.Identifier.ValueText,
            ForEachStatementSyntax forEachStatement => "loop_" + forEachStatement.Identifier.ValueText,
            _ => "outer",
        };

        var existing = GetExistingLabelNames(loop);
        if (!existing.Contains(baseName))
            return baseName;

        for (var suffix = 2; ; suffix++)
        {
            var candidate = baseName + suffix;
            if (!existing.Contains(candidate))
                return candidate;
        }
    }

    private static HashSet<string> GetExistingLabelNames(StatementSyntax loop)
    {
        var result = new HashSet<string>();
        var member = loop.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (member != null)
        {
            foreach (var label in member.DescendantNodes().OfType<LabeledStatementSyntax>())
                result.Add(label.Identifier.ValueText);
        }

        return result;
    }

    private static bool TryGetAssignmentToTrueSite(
        IdentifierNameSyntax name,
        out ExpressionStatementSyntax assignment,
        out BreakStatementSyntax innerBreak)
    {
        assignment = null!;
        innerBreak = null!;

        if (name.Parent is not AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignmentExpression ||
            assignmentExpression.Left != name ||
            assignmentExpression.Right is not LiteralExpressionSyntax rhs ||
            rhs.Kind() != SyntaxKind.TrueLiteralExpression ||
            assignmentExpression.Parent is not ExpressionStatementSyntax assignmentStatement ||
            assignmentStatement.Parent is not BlockSyntax block)
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

    private static bool TryGetGuard(IdentifierNameSyntax name, out IfStatementSyntax ifStatement)
    {
        ifStatement = null!;

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
