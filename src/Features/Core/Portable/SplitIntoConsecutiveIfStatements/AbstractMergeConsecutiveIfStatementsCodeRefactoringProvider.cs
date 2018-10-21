// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements
{
    internal abstract class AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider<
        TExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract string IfKeywordText { get; }

        protected abstract bool IsTokenOfIfStatement(SyntaxToken token, out SyntaxNode ifStatement);

        protected abstract bool IsElseClauseOfIfStatement(SyntaxNode statement, out SyntaxNode ifStatement);

        protected abstract bool IsIfStatement(SyntaxNode statement);

        protected abstract bool HasElseClauses(SyntaxNode ifStatement);

        protected abstract SyntaxNode MergeIfStatements(
            SyntaxNode parentIfStatement, SyntaxNode ifStatement, TExpressionSyntax condition);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            if (context.Span.Length > 0 &&
                context.Span != token.Span)
            {
                return;
            }

            var syntaxFacts = context.Document.GetLanguageService<ISyntaxFactsService>();

            if (IsTokenOfIfStatement(token, out var ifStatement) &&
                (CanBeMergedWithParent(syntaxFacts, ifStatement) ||
                 await CanBeMergedWithPreviousStatementAsync(context.Document, syntaxFacts, ifStatement, context.CancellationToken)))
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        c => FixAsync(context.Document, context.Span, syntaxFacts, c),
                        IfKeywordText));
            }
        }

        private async Task<Document> FixAsync(Document document, TextSpan span, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            Contract.ThrowIfFalse(IsTokenOfIfStatement(token, out var ifStatement));

            var previousIfStatement = IsElseClauseOfIfStatement(ifStatement, out var parentIfStatement)
                ? parentIfStatement
                : GetPreviousStatement(syntaxFacts, ifStatement);

            var generator = document.GetLanguageService<SyntaxGenerator>();

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

            return document.WithSyntaxRoot(newRoot);
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
                // There is no code to run, so we can safely merge.
                return true;
            }
            else
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var controlFlow = semanticModel.AnalyzeControlFlow(insideStatements.First(), insideStatements.Last());

                return !controlFlow.EndPointIsReachable;
            }
        }

        private static SyntaxNode GetPreviousStatement(ISyntaxFactsService syntaxFacts, SyntaxNode statement)
        {
            if (!syntaxFacts.IsExecutableBlock(statement.Parent))
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

        private static IReadOnlyList<SyntaxNode> WalkDownBlocks(ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            while (statements.Count == 1 && syntaxFacts.IsPureBlock(statements[0]))
            {
                statements = syntaxFacts.GetExecutableBlockStatements(statements[0]);
            }

            return statements;
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
