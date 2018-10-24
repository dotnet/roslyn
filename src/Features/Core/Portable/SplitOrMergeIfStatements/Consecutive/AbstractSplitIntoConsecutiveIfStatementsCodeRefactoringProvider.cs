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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider<TExpressionSyntax>
        : AbstractSplitIfStatementCodeRefactoringProvider<TExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
    {
        protected override int GetLogicalExpressionKind(IIfStatementSyntaxService ifSyntaxService)
            => ifSyntaxService.LogicalOrExpressionKind;

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode ifStatement,
            TExpressionSyntax left,
            TExpressionSyntax right,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifSyntaxService = document.GetLanguageService<IIfStatementSyntaxService>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            left = left.WithAdditionalAnnotations(Formatter.Annotation);
            right = right.WithAdditionalAnnotations(Formatter.Annotation);

            root = root.TrackNodes(ifStatement);
            root = root.ReplaceNode(
                root.GetCurrentNode(ifStatement),
                ifSyntaxService.WithCondition(root.GetCurrentNode(ifStatement), left));

            var editor = new SyntaxEditor(root, generator);

            if (await CanBeSeparateStatementsAsync(document, syntaxFacts, ifStatement, cancellationToken).ConfigureAwait(false))
            {
                var secondIfStatement = ifSyntaxService.WithCondition(ifStatement, right)
                    .WithPrependedLeadingTrivia(generator.ElasticCarriageReturnLineFeed);

                editor.InsertAfter(root.GetCurrentNode(ifStatement), secondIfStatement);
            }
            else
            {
                var elseIfClause = ifSyntaxService.WithCondition(ifSyntaxService.ToElseIfClause(ifStatement), right);

                ifSyntaxService.InsertElseIfClause(editor, root.GetCurrentNode(ifStatement), elseIfClause);
            }

            return editor.GetChangedRoot();
        }

        private async Task<bool> CanBeSeparateStatementsAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifStatement,
            CancellationToken cancellationToken)
        {
            var ifSyntaxService = document.GetLanguageService<IIfStatementSyntaxService>();

            if (ifSyntaxService.GetElseLikeClauses(ifStatement).Length > 0)
            {
                return false;
            }

            if (!syntaxFacts.IsExecutableStatement(ifStatement) ||
                !syntaxFacts.IsExecutableBlock(ifStatement.Parent))
            {
                return false;
            }

            var insideStatements = syntaxFacts.GetStatementContainerStatements(ifStatement);
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
