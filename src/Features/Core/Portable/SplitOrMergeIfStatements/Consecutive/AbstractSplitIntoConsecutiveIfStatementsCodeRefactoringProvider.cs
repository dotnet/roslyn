// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
            => CodeAction.Create(
                string.Format(FeaturesResources.Split_into_consecutive_0_statements, ifKeywordText),
                createChangedDocument,
                nameof(FeaturesResources.Split_into_consecutive_0_statements) + "_" + ifKeywordText);

        protected sealed override async Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode ifOrElseIf,
            SyntaxNode leftCondition,
            SyntaxNode rightCondition,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var blockFacts = document.GetLanguageService<IBlockFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            leftCondition = leftCondition.WithAdditionalAnnotations(Formatter.Annotation);
            rightCondition = rightCondition.WithAdditionalAnnotations(Formatter.Annotation);

            var editor = new SyntaxEditor(root, generator);

            editor.ReplaceNode(ifOrElseIf, (currentNode, _) => ifGenerator.WithCondition(currentNode, leftCondition));

            if (await CanBeSeparateStatementsAsync(document, blockFacts, ifGenerator, ifOrElseIf, cancellationToken).ConfigureAwait(false))
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

                if (!blockFacts.IsExecutableBlock(ifOrElseIf.Parent))
                {
                    // In order to insert a new statement, we have to be inside a block.
                    editor.ReplaceNode(ifOrElseIf, (currentNode, _) => generator.ScopeBlock(ImmutableArray.Create(currentNode)));
                }

                editor.InsertAfter(ifOrElseIf, secondIfStatement);
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

                ifGenerator.InsertElseIfClause(editor, ifOrElseIf, elseIfClause);
            }

            return editor.GetChangedRoot();
        }

        private static async Task<bool> CanBeSeparateStatementsAsync(
            Document document,
            IBlockFactsService blockFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            CancellationToken cancellationToken)
        {
            // In order to make separate statements, ifOrElseIf must be an if statement, not an else-if clause.
            if (ifGenerator.IsElseIfClause(ifOrElseIf, out _))
            {
                return false;
            }

            // If there is an else clause, we *could* in theory separate these and move the current else clause to the second
            // statement, but we won't. It would break the else-if chain in an odd way. We'll insert an else-if instead.
            if (ifGenerator.GetElseIfAndElseClauses(ifOrElseIf).Length > 0)
            {
                return false;
            }

            var insideStatements = blockFacts.GetStatementContainerStatements(ifOrElseIf);
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
    }
}
