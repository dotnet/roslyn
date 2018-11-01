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

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifLikeStatement, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            return CanBeMergedWithParent(syntaxFacts, ifGenerator, ifLikeStatement) ||
                   await CanBeMergedWithPreviousStatementAsync(document, syntaxFacts, ifGenerator, ifLikeStatement, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode ifLikeStatement)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            var isElseIfClause = ifGenerator.IsElseIfClause(ifLikeStatement, out var parentIfLikeStatement);
            var previousIfLikeStatement = isElseIfClause ? parentIfLikeStatement : GetPreviousStatement(syntaxFacts, ifLikeStatement);

            var newCondition = generator.LogicalOrExpression(
                ifGenerator.GetCondition(previousIfLikeStatement),
                ifGenerator.GetCondition(ifLikeStatement));

            newCondition = newCondition.WithAdditionalAnnotations(Formatter.Annotation);

            root = root.TrackNodes(previousIfLikeStatement, ifLikeStatement);
            root = root.ReplaceNode(
                root.GetCurrentNode(previousIfLikeStatement),
                ifGenerator.WithCondition(root.GetCurrentNode(previousIfLikeStatement), newCondition));

            var editor = new SyntaxEditor(root, generator);

            if (isElseIfClause)
            {
                // We have:
                //    if (a)
                //        Console.WriteLine();
                //    else if (b)
                //        Console.WriteLine();

                // Remove the else-if clause and preserve any subsequent clauses.

                ifGenerator.RemoveElseIfClause(editor, root.GetCurrentNode(ifLikeStatement));
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
                Debug.Assert(syntaxFacts.IsExecutableStatement(ifLikeStatement));
                Debug.Assert(syntaxFacts.IsExecutableStatement(previousIfLikeStatement));
                Debug.Assert(ifGenerator.GetElseLikeClauses(previousIfLikeStatement).Length == 0);

                editor.ReplaceNode(
                    root.GetCurrentNode(previousIfLikeStatement),
                    ifGenerator.WithElseLikeClausesOf(root.GetCurrentNode(previousIfLikeStatement), ifLikeStatement));

                editor.RemoveNode(root.GetCurrentNode(ifLikeStatement));
            }

            return editor.GetChangedRoot();
        }

        private bool CanBeMergedWithParent(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifLikeStatement)
        {
            return ifGenerator.IsElseIfClause(ifLikeStatement, out var parentIfLikeStatement) &&
                   ContainEquivalentStatements(syntaxFacts, ifLikeStatement, parentIfLikeStatement, out _);
        }

        private async Task<bool> CanBeMergedWithPreviousStatementAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifLikeStatement,
            CancellationToken cancellationToken)
        {
            // If the if-like statement is an else-if clause or we're not inside a block, there is no previous statement to merge with.
            if (!syntaxFacts.IsExecutableStatement(ifLikeStatement) ||
                !syntaxFacts.IsExecutableBlock(ifLikeStatement.Parent))
            {
                return false;
            }

            var previousStatement = GetPreviousStatement(syntaxFacts, ifLikeStatement);

            // We don't support cases where the previous if statement has any else-if or else clauses. In order for that
            // to be mergable, the control flow would have to quit from inside every branch, which is getting a little complex.
            if (!ifGenerator.IsIfLikeStatement(previousStatement) || ifGenerator.GetElseLikeClauses(previousStatement).Length > 0)
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
            var statements1 = WalkDownPureBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(ifStatement1));
            var statements2 = WalkDownPureBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(ifStatement2));

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
