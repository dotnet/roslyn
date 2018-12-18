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
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeNestedIfStatementsCodeRefactoringProvider
        : AbstractMergeIfStatementsCodeRefactoringProvider
    {
        // Converts:
        //    if (a)
        //    {
        //        if (b)
        //            Console.WriteLine();
        //    }
        //
        // To:
        //    if (a && b)
        //        Console.WriteLine();

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override async Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifStatement, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            return IsFirstStatementOfIfOrElseIf(syntaxFacts, ifGenerator, ifStatement, out var outerIfOrElseIf) &&
                   await CanBeMergedWithOuterAsync(document, syntaxFacts, ifGenerator, outerIfOrElseIf, ifStatement, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode ifStatement)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            Contract.ThrowIfFalse(IsFirstStatementOfIfOrElseIf(syntaxFacts, ifGenerator, ifStatement, out var outerIfOrElseIf));

            var newCondition = generator.LogicalAndExpression(
                ifGenerator.GetCondition(outerIfOrElseIf),
                ifGenerator.GetCondition(ifStatement));

            var newIfOrElseIf = ifGenerator.WithStatementsOf(
                ifGenerator.WithCondition(outerIfOrElseIf, newCondition),
                ifStatement);

            return root.ReplaceNode(outerIfOrElseIf, newIfOrElseIf.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private bool IsFirstStatementOfIfOrElseIf(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode statement,
            out SyntaxNode ifOrElseIf)
        {
            // Check whether the statement is a first statement inside an if statement.
            // If it's inside a block, it has to be the first statement of the block.

            Debug.Assert(syntaxFacts.IsStatementContainer(statement.Parent));

            // A statement should always be in a statement container, but we'll do a defensive check anyway so that
            // we don't crash if the helper is missing some cases or there's a new language feature it didn't account for.
            if (syntaxFacts.IsStatementContainer(statement.Parent))
            {
                var statements = syntaxFacts.GetStatementContainerStatements(statement.Parent);
                if (statements.Count > 0 && statements[0] == statement)
                {
                    var rootStatements = WalkUpPureBlocks(syntaxFacts, statements);
                    if (rootStatements.Count > 0 && ifGenerator.IsIfOrElseIf(rootStatements[0].Parent))
                    {
                        ifOrElseIf = rootStatements[0].Parent;
                        return true;
                    }
                }
            }

            ifOrElseIf = null;
            return false;
        }

        private async Task<bool> CanBeMergedWithOuterAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode outerIfOrElseIf,
            SyntaxNode innerIfStatement,
            CancellationToken cancellationToken)
        {
            // We can only merge this with the outer if statement if any inner else-if and else clauses are equal
            // to else-if and else clauses following the outer if statement because we'll be removing them.
            if (!ifGenerator.GetElseIfAndElseClauses(outerIfOrElseIf).SequenceEqual(
                    ifGenerator.GetElseIfAndElseClauses(innerIfStatement), (a, b) => IsElseIfOrElseClauseEquivalent(syntaxFacts, ifGenerator, a, b)))
            {
                return false;
            }

            var statements = syntaxFacts.GetStatementContainerStatements(innerIfStatement.Parent);
            if (statements.Count == 1)
            {
                // There are no other statements below the inner if statement. Merging is OK.
                return true;
            }
            else
            {
                // There are statements below the inner if statement. We can merge if
                // 1. there are equivalent statements below the outer 'if', and
                // 2. control flow can't reach the end of these statements (otherwise, it would continue
                //    below the outer 'if' and run the same statements twice).
                // This will typically look like a single return, break, continue or a throw statement.
                // The opposite refactoring (SplitIntoNestedIfStatements) never generates this but we support it anyway.

                // Example:
                //    if (a)
                //    {
                //        if (b)
                //            Console.WriteLine();
                //        return;
                //    }
                //    return;

                // If we have an else-if, get the topmost if statement.
                var outerIfStatement = ifGenerator.GetRootIfStatement(outerIfOrElseIf);

                Debug.Assert(syntaxFacts.IsStatementContainer(outerIfStatement.Parent));

                // A statement should always be in a statement container, but we'll do a defensive check anyway so that
                // we don't crash if the helper is missing some cases or there's a new language feature it didn't account for.
                if (!syntaxFacts.IsStatementContainer(outerIfStatement.Parent))
                {
                    return false;
                }

                var outerStatements = syntaxFacts.GetStatementContainerStatements(outerIfStatement.Parent);
                var outerIfStatementIndex = outerStatements.IndexOf(outerIfStatement);

                var remainingStatements = statements.Skip(1);
                var remainingOuterStatements = outerStatements.Skip(outerIfStatementIndex + 1);

                if (!remainingStatements.SequenceEqual(remainingOuterStatements.Take(statements.Count - 1), syntaxFacts.AreEquivalent))
                {
                    return false;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var controlFlow = semanticModel.AnalyzeControlFlow(statements[0], statements[statements.Count - 1]);

                return !controlFlow.EndPointIsReachable;
            }
        }

        private bool IsElseIfOrElseClauseEquivalent(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode elseIfOrElseClause1,
            SyntaxNode elseIfOrElseClause2)
        {
            // Compare Else/ElseIf clauses for equality.

            var isIfStatement = ifGenerator.IsIfOrElseIf(elseIfOrElseClause1);
            if (isIfStatement != ifGenerator.IsIfOrElseIf(elseIfOrElseClause2))
            {
                // If we have one Else and one ElseIf, they're not equal.
                return false;
            }

            if (isIfStatement)
            {
                // If we have two ElseIf blocks, their conditions have to match.
                var condition1 = ifGenerator.GetCondition(elseIfOrElseClause1);
                var condition2 = ifGenerator.GetCondition(elseIfOrElseClause2);

                if (!syntaxFacts.AreEquivalent(condition1, condition2))
                {
                    return false;
                }
            }

            var statements1 = WalkDownPureBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(elseIfOrElseClause1));
            var statements2 = WalkDownPureBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(elseIfOrElseClause2));

            return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Merge_nested_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
