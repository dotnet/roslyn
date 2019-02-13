// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractMergeIfStatementsCodeRefactoringProvider
    {
        // Converts:
        //    if (a)
        //        Console.WriteLine();
        //    else if (b)
        //        Console.WriteLine();
        //
        // To:
        //    if (a || b)
        //        Console.WriteLine();

        // Converts:
        //    if (a)
        //        return;
        //    if (b)
        //        return;
        //
        // To:
        //    if (a || b)
        //        return;

        // The body statements need to be equivalent. In the second case, control flow must quit from inside the body.

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, MergeDirection direction, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, direction, ifKeywordText);

        protected sealed override Task<bool> CanBeMergedUpAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode firstIfOrElseIf)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            if (CanBeMergedWithParent(syntaxFacts, ifGenerator, ifOrElseIf, out firstIfOrElseIf))
                return Task.FromResult(true);

            return CanBeMergedWithPreviousStatementAsync(document, syntaxFacts, ifGenerator, ifOrElseIf, cancellationToken, out firstIfOrElseIf);
        }

        protected sealed override Task<bool> CanBeMergedDownAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode secondIfOrElseIf)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            if (CanBeMergedWithElseIf(syntaxFacts, ifGenerator, ifOrElseIf, out secondIfOrElseIf))
                return Task.FromResult(true);

            return CanBeMergedWithNextStatementAsync(document, syntaxFacts, ifGenerator, ifOrElseIf, cancellationToken, out secondIfOrElseIf);
        }

        protected sealed override SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode firstIfOrElseIf, SyntaxNode secondIfOrElseIf)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            var newCondition = generator.LogicalOrExpression(
                ifGenerator.GetCondition(firstIfOrElseIf),
                ifGenerator.GetCondition(secondIfOrElseIf));

            newCondition = newCondition.WithAdditionalAnnotations(Formatter.Annotation);

            var editor = new SyntaxEditor(root, generator);

            editor.ReplaceNode(firstIfOrElseIf, (currentNode, _) => ifGenerator.WithCondition(currentNode, newCondition));

            if (ifGenerator.IsElseIfClause(secondIfOrElseIf, out _))
            {
                // We have:
                //    if (a)
                //        Console.WriteLine();
                //    else if (b)
                //        Console.WriteLine();

                // Remove the else-if clause and preserve any subsequent clauses.

                ifGenerator.RemoveElseIfClause(editor, secondIfOrElseIf);
            }
            else
            {
                // We have:
                //    if (a)
                //        return;
                //    if (b)
                //        return;

                // At this point, ifLikeStatement must be a standalone if statement, possibly with an else clause (there won't
                // be any on the first statement though). We'll move any else-if and else clauses to the first statement
                // and then remove the second one.
                // The opposite refactoring (SplitIntoConsecutiveIfStatements) never generates a separate statement
                // with an else clause but we support it anyway (in inserts an else-if instead).
                Debug.Assert(syntaxFacts.IsExecutableStatement(secondIfOrElseIf));
                Debug.Assert(syntaxFacts.IsExecutableStatement(firstIfOrElseIf));
                Debug.Assert(ifGenerator.GetElseIfAndElseClauses(firstIfOrElseIf).Length == 0);

                editor.ReplaceNode(
                    firstIfOrElseIf,
                    (currentNode, _) => ifGenerator.WithElseIfAndElseClausesOf(currentNode, secondIfOrElseIf));

                editor.RemoveNode(secondIfOrElseIf);
            }

            return editor.GetChangedRoot();
        }

        private bool CanBeMergedWithParent(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            out SyntaxNode parentIfOrElseIf)
        {
            return ifGenerator.IsElseIfClause(ifOrElseIf, out parentIfOrElseIf) &&
                   ContainEquivalentStatements(syntaxFacts, ifOrElseIf, parentIfOrElseIf, out _);
        }

        private bool CanBeMergedWithElseIf(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            out SyntaxNode elseIfClause)
        {
            return ifGenerator.HasElseIfClause(ifOrElseIf, out elseIfClause) &&
                   ContainEquivalentStatements(syntaxFacts, ifOrElseIf, elseIfClause, out _);
        }

        private Task<bool> CanBeMergedWithPreviousStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            CancellationToken cancellationToken,
            out SyntaxNode previousStatement)
        {
            return TryGetSiblingStatement(syntaxFacts, ifOrElseIf, relativeIndex: -1, out previousStatement)
                ? CanStatementsBeMergedAsync(document, syntaxFacts, ifGenerator, previousStatement, ifOrElseIf, cancellationToken)
                : Task.FromResult(false);
        }

        private Task<bool> CanBeMergedWithNextStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            CancellationToken cancellationToken,
            out SyntaxNode nextStatement)
        {
            return TryGetSiblingStatement(syntaxFacts, ifOrElseIf, relativeIndex: 1, out nextStatement)
                ? CanStatementsBeMergedAsync(document, syntaxFacts, ifGenerator, ifOrElseIf, nextStatement, cancellationToken)
                : Task.FromResult(false);
        }

        private async Task<bool> CanStatementsBeMergedAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode firstStatement,
            SyntaxNode secondStatement,
            CancellationToken cancellationToken)
        {
            // We don't support cases where the previous if statement has any else-if or else clauses. In order for that
            // to be mergable, the control flow would have to quit from inside every branch, which is getting a little complex.
            if (!ifGenerator.IsIfOrElseIf(firstStatement) || ifGenerator.GetElseIfAndElseClauses(firstStatement).Length > 0)
                return false;

            if (!ifGenerator.IsIfOrElseIf(secondStatement))
                return false;

            if (!ContainEquivalentStatements(syntaxFacts, firstStatement, secondStatement, out var insideStatements))
                return false;

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
                var controlFlow = semanticModel.AnalyzeControlFlow(insideStatements[0], insideStatements[insideStatements.Count - 1]);

                return !controlFlow.EndPointIsReachable;
            }
        }

        private static bool TryGetSiblingStatement(
            ISyntaxFactsService syntaxFacts, SyntaxNode ifOrElseIf, int relativeIndex, out SyntaxNode statement)
        {
            if (syntaxFacts.IsExecutableStatement(ifOrElseIf) &&
                syntaxFacts.IsExecutableBlock(ifOrElseIf.Parent))
            {
                var blockStatements = syntaxFacts.GetExecutableBlockStatements(ifOrElseIf.Parent);

                statement = blockStatements.ElementAtOrDefault(blockStatements.IndexOf(ifOrElseIf) + relativeIndex);
                return statement != null;
            }

            statement = default;
            return false;
        }

        private static bool ContainEquivalentStatements(
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifStatement1,
            SyntaxNode ifStatement2,
            out IReadOnlyList<SyntaxNode> statements)
        {
            var statements1 = WalkDownScopeBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(ifStatement1));
            var statements2 = WalkDownScopeBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(ifStatement2));

            statements = statements1;
            return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, MergeDirection direction, string ifKeywordText)
                : base(string.Format(GetResourceText(direction), ifKeywordText), createChangedDocument)
            {
            }

            private static string GetResourceText(MergeDirection direction)
                => direction == MergeDirection.Up ? FeaturesResources.Merge_with_previous_0_statement : FeaturesResources.Merge_with_next_0_statement;
        }
    }
}
