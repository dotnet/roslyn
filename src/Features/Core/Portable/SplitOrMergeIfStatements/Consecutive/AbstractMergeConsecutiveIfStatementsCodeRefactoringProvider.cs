// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider<TExpressionSyntax>
        : AbstractMergeIfStatementsCodeRefactoringProvider<TExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract bool IsElseClauseOfIfStatement(SyntaxNode node, out SyntaxNode ifStatementNode);

        protected abstract bool HasElseClauses(SyntaxNode ifStatementNode);

        protected abstract SyntaxNode MergeIfStatements(
            SyntaxNode firstIfStatementNode, SyntaxNode secondIfStatementNode, TExpressionSyntax condition);

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            return CanBeMergedWithParent(syntaxFacts, ifStatement) ||
                   await CanBeMergedWithPreviousStatementAsync(document, syntaxFacts, ifStatement, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override SyntaxNode GetChangedRoot(
            SyntaxNode root, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts, SyntaxGenerator generator)
        {
            var previousIfStatement = IsElseClauseOfIfStatement(ifStatement, out var parentIfStatement)
                ? parentIfStatement
                : GetPreviousStatement(syntaxFacts, ifStatement);

            var newCondition = (TExpressionSyntax)generator.LogicalOrExpression(
                syntaxFacts.GetIfStatementCondition(previousIfStatement),
                syntaxFacts.GetIfStatementCondition(ifStatement));

            var newIfStatement = MergeIfStatements(previousIfStatement, ifStatement, newCondition);

            var newRoot = root.TrackNodes(previousIfStatement, ifStatement);
            newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(previousIfStatement), newIfStatement.WithAdditionalAnnotations(Formatter.Annotation));

            var currentIfStatement = newRoot.GetCurrentNode(ifStatement);
            if (currentIfStatement != null)
            {
                newRoot = newRoot.RemoveNode(currentIfStatement, SyntaxGenerator.DefaultRemoveOptions);
            }

            return newRoot;
        }

        private bool CanBeMergedWithParent(ISyntaxFactsService syntaxFacts, SyntaxNode ifStatement)
        {
            return IsElseClauseOfIfStatement(ifStatement, out var parentIfStatement) &&
                   ContainEquivalentStatements(syntaxFacts, ifStatement, parentIfStatement, out _);
        }

        private async Task<bool> CanBeMergedWithPreviousStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifStatement,
            CancellationToken cancellationToken)
        {
            if (HasElseClauses(ifStatement))
            {
                return false;
            }

            var previousStatement = GetPreviousStatement(syntaxFacts, ifStatement);

            if (!IsIfStatement(previousStatement) || HasElseClauses(previousStatement))
            {
                return false;
            }

            if (!ContainEquivalentStatements(syntaxFacts, ifStatement, previousStatement, out var insideStatements))
            {
                return false;
            }

            if (insideStatements.Count == 0)
            {
                // Even though there are no statements inside, we still can't merge these into one statement
                // because it would change the semantics from always evaluating the second condition to short-circuiting.
                return false;
            }
            else
            {
                // There are statements inside. We can merge these into one statement if
                // control flow can't reach the end of these statements (otherwise, it would change from running
                // the second 'if' in the case that both conditions are true to only running the statements once).
                // This will typically look like a single return, break, continue or a throw statement.

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var controlFlow = semanticModel.AnalyzeControlFlow(insideStatements.First(), insideStatements.Last());

                return !controlFlow.EndPointIsReachable;
            }
        }

        private static SyntaxNode GetPreviousStatement(ISyntaxFactsService syntaxFacts, SyntaxNode statement)
        {
            if (!syntaxFacts.IsExecutableStatement(statement) ||
                !syntaxFacts.IsExecutableBlock(statement.Parent))
            {
                return null;
            }

            var blockStatements = syntaxFacts.GetExecutableBlockStatements(statement.Parent);
            var statementIndex = blockStatements.IndexOf(statement);

            return blockStatements.ElementAtOrDefault(statementIndex - 1);
        }

        private static bool ContainEquivalentStatements(
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifStatement1,
            SyntaxNode ifStatement2,
            out IReadOnlyList<SyntaxNode> statements)
        {
            var statements1 = WalkDownBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(ifStatement1));
            var statements2 = WalkDownBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(ifStatement2));

            statements = statements1;
            return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Merge_consecutive_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
