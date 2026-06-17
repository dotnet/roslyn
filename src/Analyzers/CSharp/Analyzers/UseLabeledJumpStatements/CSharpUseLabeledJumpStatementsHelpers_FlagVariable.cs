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
    /// A <c>bool</c> flag emulating a multi-level break/continue (the guard decides which):
    /// <code>
    /// bool flag = false;                        // Declaration
    /// while (...)                               // Loop
    /// {
    ///     while (...)
    ///         if (...) { flag = true; break; }  // Sites
    ///     if (flag) break;                      // Guard (or 'continue')
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
            !outerLoop.IsContinuableConstruct())
        {
            return false;
        }

        var guardIndex = guardBlock.Statements.IndexOf(guard);
        if (guardIndex <= 0)
            return false;

        // A prior fix may have already labeled this inner loop ('loop_j: for (...)'); unwrap to the loop itself.
        var innerLoop = guardBlock.Statements[guardIndex - 1];
        if (innerLoop is LabeledStatementSyntax labeledInner)
            innerLoop = labeledInner.Statement;

        if (!innerLoop.IsContinuableConstruct())
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
            LocalDeclarationStatement = declaration,
            LoopStatement = outerLoop,
            GuardStatement = guard,
            Sites = sites.ToImmutable(),
            IsBreak = isBreak,
        };
        return true;
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
