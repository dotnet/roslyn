// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode;

internal static class RemoveUnreachableCodeHelpers
{
    public static ImmutableArray<ImmutableArray<StatementSyntax>> GetSubsequentUnreachableSections(StatementSyntax firstUnreachableStatement)
    {
        ImmutableArray<StatementSyntax> siblingStatements;
        BlockSyntax? unreachableStatementContainingBlock = null;
        switch (firstUnreachableStatement.Parent)
        {
            case BlockSyntax block:
                var topmostConsecutiveBlockParent = block;
                while (true)
                {
                    var topmostParent = topmostConsecutiveBlockParent.Parent;
                    if (!topmostParent.IsKind(SyntaxKind.Block))
                    {
                        break;
                    }
                    topmostConsecutiveBlockParent = (BlockSyntax)topmostParent;
                }

                unreachableStatementContainingBlock = topmostConsecutiveBlockParent;
                siblingStatements = [.. unreachableStatementContainingBlock.Statements];
                break;

            case SwitchSectionSyntax switchSection:
                siblingStatements = [.. switchSection.Statements];
                break;

            case GlobalStatementSyntax globalStatement:
                if (globalStatement.Parent is not CompilationUnitSyntax compilationUnit)
                {
                    return [];
                }

                {
                    // Can't use `SyntaxList<TNode>` here since the retrieving the added node will result
                    // in a different reference.
                    using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var builder);
                    foreach (var member in compilationUnit.Members)
                    {
                        if (member is not GlobalStatementSyntax currentGlobalStatement)
                        {
                            continue;
                        }

                        builder.Add(currentGlobalStatement.Statement);
                    }

                    siblingStatements = builder.ToImmutable();
                }

                break;

            default:
                // We're an embedded statement.  So the unreachable section is just us.
                return [];
        }

        var sections = ArrayBuilder<ImmutableArray<StatementSyntax>>.GetInstance();

        var firstStatementSpan = firstUnreachableStatement.Span;

        var currentSection = ArrayBuilder<StatementSyntax>.GetInstance();
        var firstUnreachableStatementIndex = siblingStatements.IndexOf(s => s.Span.Contains(firstStatementSpan));

        // Since the first unreachable statement may be contained inside a nested block,
        // we iterate from the first statement that contains the first unreachable statement,
        // which could either be the unreachable statement itself, or any of its ancestor blocks.
        // The unreachable statement itself and all the previous ones will not be included in any
        // of the sections.
        for (int i = firstUnreachableStatementIndex, n = siblingStatements.Length; i < n; i++)
        {
            var currentStatement = siblingStatements[i];
            if (currentStatement.IsKind(SyntaxKind.LabeledStatement))
            {
                // In the case of a subsequent labeled statement, we don't want to consider it 
                // unreachable as there may be a 'goto' somewhere else to that label.  If the 
                // compiler actually thinks that label is unreachable, it will give an diagnostic 
                // on that label itself  and we can use that diagnostic to handle it and any 
                // subsequent sections.
                break;
            }

            if (currentStatement.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                // In the case of local functions, it is legal for a local function to be declared
                // in code that is otherwise unreachable.  It can still be called elsewhere.  If
                // the local function itself is not called, there will be a particular diagnostic
                // for that ("The variable XXX is declared but never used") and the user can choose
                // if they want to remove it or not.
                ConsumeCurrentSection();
                continue;
            }

            if (i > firstUnreachableStatementIndex)
            {
                currentSection.Add(currentStatement);
                continue;
            }

            if (currentStatement.IsKind(SyntaxKind.Block))
            {
                ProcessBlock((BlockSyntax)currentStatement);
                continue;
            }

            void ProcessBlock(BlockSyntax block)
            {
                // In the case of raw blocks that are not part of other statements, like if, for, etc.
                // we want to report all its contained statements as distinct unreachable segments, but
                // only the statements that come after the first unreachable statement
                ConsumeCurrentSection();

                foreach (var statement in block.Statements)
                {
                    if (statement.IsKind(SyntaxKind.Block))
                    {
                        var innerBlock = (BlockSyntax)statement;
                        if (innerBlock.SpanStart > firstStatementSpan.Start || innerBlock.Span.Contains(firstStatementSpan))
                        {
                            ProcessBlock(innerBlock);
                        }
                        continue;
                    }

                    if (statement.SpanStart > firstStatementSpan.Start)
                    {
                        currentSection.Add(statement);
                    }
                }

                ConsumeCurrentSection();
            }

            void ConsumeCurrentSection()
            {
                var section = currentSection.ToImmutableAndFree();
                AddIfNonEmpty(sections, section);

                currentSection = ArrayBuilder<StatementSyntax>.GetInstance();
            }
        }

        var lastSection = currentSection.ToImmutableAndFree();
        AddIfNonEmpty(sections, lastSection);

        return sections.ToImmutableAndFree();
    }

    private static void AddIfNonEmpty(ArrayBuilder<ImmutableArray<StatementSyntax>> sections, ImmutableArray<StatementSyntax> lastSection)
    {
        if (!lastSection.IsEmpty)
        {
            sections.Add(lastSection);
        }
    }
}
