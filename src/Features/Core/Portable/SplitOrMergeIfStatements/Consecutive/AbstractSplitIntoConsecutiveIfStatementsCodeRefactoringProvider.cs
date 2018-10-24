// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractSplitIfStatementCodeRefactoringProvider
    {
        protected sealed override int GetLogicalExpressionKind(ISyntaxKindsService syntaxKinds)
            => syntaxKinds.LogicalOrExpression;

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode ifLikeStatement,
            SyntaxNode leftCondition,
            SyntaxNode rightCondition,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            leftCondition = leftCondition.WithAdditionalAnnotations(Formatter.Annotation);
            rightCondition = rightCondition.WithAdditionalAnnotations(Formatter.Annotation);

            root = root.TrackNodes(ifLikeStatement);
            root = root.ReplaceNode(
                root.GetCurrentNode(ifLikeStatement),
                ifGenerator.WithCondition(root.GetCurrentNode(ifLikeStatement), leftCondition));

            var editor = new SyntaxEditor(root, generator);

            if (await CanBeSeparateStatementsAsync(document, syntaxFacts, ifLikeStatement, cancellationToken).ConfigureAwait(false))
            {
                var secondIfStatement = ifGenerator.WithCondition(ifLikeStatement, rightCondition)
                    .WithPrependedLeadingTrivia(generator.ElasticCarriageReturnLineFeed);

                editor.InsertAfter(root.GetCurrentNode(ifLikeStatement), secondIfStatement);
            }
            else
            {
                var elseIfClause = ifGenerator.WithCondition(ifGenerator.ToElseIfClause(ifLikeStatement), rightCondition);

                ifGenerator.InsertElseIfClause(editor, root.GetCurrentNode(ifLikeStatement), elseIfClause);
            }

            return editor.GetChangedRoot();
        }

        private async Task<bool> CanBeSeparateStatementsAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifLikeStatement,
            CancellationToken cancellationToken)
        {
            // If the if-like statement is an else-if clause or we're not inside a block, we cannot introduce another statement.
            if (!syntaxFacts.IsExecutableStatement(ifLikeStatement) ||
                !syntaxFacts.IsExecutableBlock(ifLikeStatement.Parent))
            {
                return false;
            }

            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            if (ifGenerator.GetElseLikeClauses(ifLikeStatement).Length > 0)
            {
                return false;
            }

            var insideStatements = syntaxFacts.GetStatementContainerStatements(ifLikeStatement);
            if (insideStatements.Count == 0)
            {
                // Even though there are no statements inside, we still can't split this into separate statements
                // because it would change the semantics from short-circuiting to always evaluating the second condition,
                // breaking code like 'if (a == null || a.InstanceMethod())'.
                return false;
            }
            else
            {
                // There are statements inside. We can split this into separate statements and leave out the 'else' if
                // control flow can't reach the end of these statements (otherwise, it would continue to the second 'if'
                // and in the case that both conditions are true, run the same statements twice).
                // This will typically look like a single return, break, continue or a throw statement.

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var controlFlow = semanticModel.AnalyzeControlFlow(insideStatements.First(), insideStatements.Last());

                return !controlFlow.EndPointIsReachable;
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Split_into_consecutive_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
