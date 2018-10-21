// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeNestedIfStatementsCodeRefactoringProvider<
        TExpressionSyntax> : AbstractMergeIfStatementsCodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract ImmutableArray<SyntaxNode> GetElseClauses(SyntaxNode ifStatement);

        protected abstract SyntaxNode MergeIfStatements(
            SyntaxNode outerIfStatement, SyntaxNode innerIfStatement, TExpressionSyntax condition);

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
            => new MyCodeAction(createChangedDocument, IfKeywordText);

        protected sealed override async Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            return IsFirstStatementOfIfStatement(syntaxFacts, ifStatement, out var parentIfStatement) &&
                   await CanBeMergedWithOuterAsync(document, syntaxFacts, parentIfStatement, ifStatement, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override SyntaxNode GetChangedRoot(
            SyntaxNode root, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts, SyntaxGenerator generator)
        {
            Contract.ThrowIfFalse(IsFirstStatementOfIfStatement(syntaxFacts, ifStatement, out var parentIfStatement));

            var newCondition = (TExpressionSyntax)generator.LogicalAndExpression(
                syntaxFacts.GetIfStatementCondition(parentIfStatement),
                syntaxFacts.GetIfStatementCondition(ifStatement));

            var newIfStatement = MergeIfStatements(parentIfStatement, ifStatement, newCondition);

            return root.ReplaceNode(parentIfStatement, newIfStatement.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private bool IsFirstStatementOfIfStatement(
            ISyntaxFactsService syntaxFacts, SyntaxNode statement, out SyntaxNode ifStatement)
        {
            if (syntaxFacts.IsStatementContainer(statement.Parent))
            {
                var statements = syntaxFacts.GetStatementContainerStatements(statement.Parent);
                if (statements.FirstOrDefault() == statement)
                {
                    var rootStatements = WalkUpBlocks(syntaxFacts, statements);
                    if (rootStatements.Count > 0 && IsIfStatement(rootStatements[0].Parent))
                    {
                        ifStatement = rootStatements[0].Parent;
                        return true;
                    }
                }
            }

            ifStatement = null;
            return false;
        }

        private async Task<bool> CanBeMergedWithOuterAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode outerIfStatement,
            SyntaxNode innerIfStatement,
            CancellationToken cancellationToken)
        {
            if (!GetElseClauses(outerIfStatement).SequenceEqual(
                    GetElseClauses(innerIfStatement), (a, b) => IsElseClauseEquivalent(syntaxFacts, a, b)))
            {
                return false;
            }

            var statements = syntaxFacts.GetStatementContainerStatements(innerIfStatement.Parent);
            if (statements.Count == 1)
            {
                // There are no other statements below the inner if statement. Merging is OK.
                return true;
            }
            else
            {
                // There are statements below the inner if statement. We can merge if
                // 1. these statements exist below the outer if as well, and
                // 2. control flow can't reach after the end of these statements (otherwise, they would get executed twice).
                // This will typically look like a single return, break, continue or a throw statement.

                if (!syntaxFacts.IsStatementContainer(outerIfStatement.Parent))
                {
                    // This shouldn't happen, but let's be cautious.
                    return false;
                }

                var outerStatements = syntaxFacts.GetStatementContainerStatements(outerIfStatement.Parent);
                var outerIfStatementIndex = outerStatements.IndexOf(outerIfStatement);

                var remainingStatements = statements.Skip(1);
                var remainingOuterStatements = outerStatements.Skip(outerIfStatementIndex + 1);

                if (!remainingStatements.SequenceEqual(remainingOuterStatements.Take(statements.Count - 1), syntaxFacts.AreEquivalent))
                {
                    return false;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var controlFlow = semanticModel.AnalyzeControlFlow(statements.First(), statements.Last());

                return !controlFlow.EndPointIsReachable;
            }
        }

        private bool IsElseClauseEquivalent(ISyntaxFactsService syntaxFacts, SyntaxNode elseClause1, SyntaxNode elseClause2)
        {
            var isIfStatement = IsIfStatement(elseClause1);
            if (isIfStatement != IsIfStatement(elseClause2))
            {
                // If we have one ElseIf and one If, they're not equal.
                return false;
            }

            if (isIfStatement)
            {
                // If we have two ElseIf blocks, their conditions have to match.
                var condition1 = syntaxFacts.GetIfStatementCondition(elseClause1);
                var condition2 = syntaxFacts.GetIfStatementCondition(elseClause2);

                if (!syntaxFacts.AreEquivalent(condition1, condition2))
                {
                    return false;
                }
            }

            var statements1 = WalkDownBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(elseClause1));
            var statements2 = WalkDownBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(elseClause2));

            return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Merge_nested_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
