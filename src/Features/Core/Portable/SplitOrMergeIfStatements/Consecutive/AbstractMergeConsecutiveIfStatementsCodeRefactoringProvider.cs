// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

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
        {
            var resourceText = direction == MergeDirection.Up ? FeaturesResources.Merge_with_previous_0_statement : FeaturesResources.Merge_with_next_0_statement;
            var title = string.Format(resourceText, ifKeywordText);
            return CodeAction.Create(title, createChangedDocument, title);
        }

        protected sealed override Task<bool> CanBeMergedUpAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode firstIfOrElseIf)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var blockFacts = document.GetLanguageService<IBlockFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            if (CanBeMergedWithParent(syntaxFacts, blockFacts, ifGenerator, ifOrElseIf, out firstIfOrElseIf))
                return SpecializedTasks.True;

            return CanBeMergedWithPreviousStatementAsync(document, syntaxFacts, blockFacts, ifGenerator, ifOrElseIf, cancellationToken, out firstIfOrElseIf);
        }

        protected sealed override Task<bool> CanBeMergedDownAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode secondIfOrElseIf)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var blockFacts = document.GetLanguageService<IBlockFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            if (CanBeMergedWithElseIf(syntaxFacts, blockFacts, ifGenerator, ifOrElseIf, out secondIfOrElseIf))
                return SpecializedTasks.True;

            return CanBeMergedWithNextStatementAsync(document, syntaxFacts, blockFacts, ifGenerator, ifOrElseIf, cancellationToken, out secondIfOrElseIf);
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

        private static bool CanBeMergedWithParent(
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            out SyntaxNode parentIfOrElseIf)
        {
            return ifGenerator.IsElseIfClause(ifOrElseIf, out parentIfOrElseIf) &&
                   ContainEquivalentStatements(syntaxFacts, blockFacts, ifOrElseIf, parentIfOrElseIf, out _);
        }

        private static bool CanBeMergedWithElseIf(
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            out SyntaxNode elseIfClause)
        {
            return ifGenerator.HasElseIfClause(ifOrElseIf, out elseIfClause) &&
                   ContainEquivalentStatements(syntaxFacts, blockFacts, ifOrElseIf, elseIfClause, out _);
        }

        private static Task<bool> CanBeMergedWithPreviousStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            CancellationToken cancellationToken,
            out SyntaxNode previousStatement)
        {
            return TryGetSiblingStatement(syntaxFacts, blockFacts, ifOrElseIf, relativeIndex: -1, out previousStatement)
                ? CanStatementsBeMergedAsync(document, syntaxFacts, blockFacts, ifGenerator, previousStatement, ifOrElseIf, cancellationToken)
                : SpecializedTasks.False;
        }

        private static Task<bool> CanBeMergedWithNextStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            CancellationToken cancellationToken,
            out SyntaxNode nextStatement)
        {
            return TryGetSiblingStatement(syntaxFacts, blockFacts, ifOrElseIf, relativeIndex: 1, out nextStatement)
                ? CanStatementsBeMergedAsync(document, syntaxFacts, blockFacts, ifGenerator, ifOrElseIf, nextStatement, cancellationToken)
                : SpecializedTasks.False;
        }

        private static async Task<bool> CanStatementsBeMergedAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
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

            if (!ContainEquivalentStatements(syntaxFacts, blockFacts, firstStatement, secondStatement, out var insideStatements))
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
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
            SyntaxNode ifOrElseIf,
            int relativeIndex,
            out SyntaxNode statement)
        {
            if (syntaxFacts.IsExecutableStatement(ifOrElseIf) &&
                blockFacts.IsExecutableBlock(ifOrElseIf.Parent))
            {
                var blockStatements = blockFacts.GetExecutableBlockStatements(ifOrElseIf.Parent);

                statement = blockStatements.ElementAtOrDefault(blockStatements.IndexOf(ifOrElseIf) + relativeIndex);
                return statement != null;
            }

            statement = null;
            return false;
        }

        private static bool ContainEquivalentStatements(
            ISyntaxFactsService syntaxFacts,
            IBlockFactsService blockFacts,
            SyntaxNode ifStatement1,
            SyntaxNode ifStatement2,
            out IReadOnlyList<SyntaxNode> statements)
        {
            var statements1 = WalkDownScopeBlocks(blockFacts, blockFacts.GetStatementContainerStatements(ifStatement1));
            var statements2 = WalkDownScopeBlocks(blockFacts, blockFacts.GetStatementContainerStatements(ifStatement2));

            statements = statements1;
            return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
        }
    }
}
