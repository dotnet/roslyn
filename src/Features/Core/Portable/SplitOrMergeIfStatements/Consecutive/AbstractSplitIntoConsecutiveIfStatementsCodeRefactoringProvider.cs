// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal abstract class AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractSplitIfStatementCodeRefactoringProvider
    {
        // Converts:
        //    if (a || b)
        //        Console.WriteLine();
        //
        // To:
        //    if (a)
        //        Console.WriteLine();
        //    else if (b)
        //        Console.WriteLine();

        // Converts:
        //    if (a || b)
        //        return;
        //
        // To:
        //    if (a)
        //        return;
        //    if (b)
        //        return;

        // The second case is applied if control flow quits from inside the body.

        protected sealed override int GetLogicalExpressionKind(ISyntaxKindsService syntaxKinds)
            => syntaxKinds.LogicalOrExpression;

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode ifOrElseIf,
            SyntaxNode leftCondition,
            SyntaxNode rightCondition,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            leftCondition = leftCondition.WithAdditionalAnnotations(Formatter.Annotation);
            rightCondition = rightCondition.WithAdditionalAnnotations(Formatter.Annotation);

            // The syntax editor will be operating on ifLikeStatement. If we did this replacement
            // using the syntax editor, it wouldn't be able to do the subsequent modifications (insert an else clause).
            // We need to do this in a separate step and track the nodes for later use.
            root = root.TrackNodes(ifOrElseIf);
            root = root.ReplaceNode(
                root.GetCurrentNode(ifOrElseIf),
                ifGenerator.WithCondition(root.GetCurrentNode(ifOrElseIf), leftCondition));

            var editor = new SyntaxEditor(root, generator);

            if (await CanBeSeparateStatementsAsync(document, syntaxFacts, ifGenerator, ifOrElseIf, cancellationToken).ConfigureAwait(false))
            {
                // Generate:
                // if (a)
                //     return;
                // if (b)
                //     return;

                // At this point, ifLikeStatement must be a standalone if statement with no else clause.
                Debug.Assert(syntaxFacts.IsExecutableStatement(ifOrElseIf));
                Debug.Assert(ifGenerator.GetElseIfAndElseClauses(ifOrElseIf).Length == 0);

                var secondIfStatement = ifGenerator.WithCondition(ifOrElseIf, rightCondition)
                    .WithPrependedLeadingTrivia(generator.ElasticCarriageReturnLineFeed);

                editor.InsertAfter(root.GetCurrentNode(ifOrElseIf), secondIfStatement);
            }
            else
            {
                // Generate:
                // if (a)
                //     Console.WriteLine();
                // else if (b)
                //     Console.WriteLine();

                // If the if statement is not an else-if clause, we convert it to an else-if clause first (for VB).
                // Then we insert it right after our current if statement or else-if clause.

                var elseIfClause = ifGenerator.WithCondition(ifGenerator.ToElseIfClause(ifOrElseIf), rightCondition);

                ifGenerator.InsertElseIfClause(editor, root.GetCurrentNode(ifOrElseIf), elseIfClause);
            }

            return editor.GetChangedRoot();
        }

        private async Task<bool> CanBeSeparateStatementsAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            CancellationToken cancellationToken)
        {
            // If the if-like statement is an else-if clause or we're not inside a block, we cannot introduce another statement.
            if (!syntaxFacts.IsExecutableStatement(ifOrElseIf) ||
                !syntaxFacts.IsExecutableBlock(ifOrElseIf.Parent))
            {
                return false;
            }

            // If there is an else clause, we *could* in theory separate these and move the current else clause to the second
            // statement, but we won't. It would break the else-if chain in an odd way. We'll insert an else-if instead.
            if (ifGenerator.GetElseIfAndElseClauses(ifOrElseIf).Length > 0)
            {
                return false;
            }

            var insideStatements = syntaxFacts.GetStatementContainerStatements(ifOrElseIf);
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
                var controlFlow = semanticModel.AnalyzeControlFlow(insideStatements[0], insideStatements[insideStatements.Count - 1]);

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
