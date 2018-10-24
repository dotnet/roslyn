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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractMergeIfStatementsCodeRefactoringProvider
    {
        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifLikeStatement, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            var ifSyntaxService = document.GetLanguageService<IIfStatementSyntaxService>();

            return CanBeMergedWithParent(syntaxFacts, ifSyntaxService, ifLikeStatement) ||
                   await CanBeMergedWithPreviousStatementAsync(document, syntaxFacts, ifLikeStatement, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode ifLikeStatement)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifSyntaxService = document.GetLanguageService<IIfStatementSyntaxService>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            var isElseIfClause = ifSyntaxService.IsElseIfClause(ifLikeStatement, out var parentIfLikeStatement);
            var previousIfLikeStatement = isElseIfClause ? parentIfLikeStatement : GetPreviousStatement(syntaxFacts, ifLikeStatement);

            var newCondition = generator.LogicalOrExpression(
                ifSyntaxService.GetCondition(previousIfLikeStatement),
                ifSyntaxService.GetCondition(ifLikeStatement));

            newCondition = newCondition.WithAdditionalAnnotations(Formatter.Annotation);

            root = root.TrackNodes(previousIfLikeStatement, ifLikeStatement);
            root = root.ReplaceNode(
                root.GetCurrentNode(previousIfLikeStatement),
                ifSyntaxService.WithCondition(root.GetCurrentNode(previousIfLikeStatement), newCondition));

            var editor = new SyntaxEditor(root, generator);

            if (isElseIfClause)
            {
                ifSyntaxService.RemoveElseIfClause(editor, root.GetCurrentNode(ifLikeStatement));
            }
            else
            {
                editor.RemoveNode(root.GetCurrentNode(ifLikeStatement));
            }

            return editor.GetChangedRoot();
        }

        private bool CanBeMergedWithParent(
            ISyntaxFactsService syntaxFacts,
            IIfStatementSyntaxService ifSyntaxService,
            SyntaxNode ifLikeStatement)
        {
            return ifSyntaxService.IsElseIfClause(ifLikeStatement, out var parentIfLikeStatement) &&
                   ContainEquivalentStatements(syntaxFacts, ifLikeStatement, parentIfLikeStatement, out _);
        }

        private async Task<bool> CanBeMergedWithPreviousStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifLikeStatement,
            CancellationToken cancellationToken)
        {
            // If the if-like statement is an else-if clause or we're not inside a block, there is no previous statement.
            if (!syntaxFacts.IsExecutableStatement(ifLikeStatement) ||
                !syntaxFacts.IsExecutableBlock(ifLikeStatement.Parent))
            {
                return false;
            }

            var ifSyntaxService = document.GetLanguageService<IIfStatementSyntaxService>();
            if (ifSyntaxService.GetElseLikeClauses(ifLikeStatement).Length > 0)
            {
                return false;
            }

            var previousStatement = GetPreviousStatement(syntaxFacts, ifLikeStatement);

            if (!ifSyntaxService.IsIfLikeStatement(previousStatement) || ifSyntaxService.GetElseLikeClauses(previousStatement).Length > 0)
            {
                return false;
            }

            if (!ContainEquivalentStatements(syntaxFacts, ifLikeStatement, previousStatement, out var insideStatements))
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
