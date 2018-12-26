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

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, MergeDirection direction, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, direction, ifKeywordText);

        protected sealed override Task<bool> CanBeMergedUpAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode outerIfOrElseIf)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            if (!IsFirstStatementOfIfOrElseIf(syntaxFacts, ifGenerator, ifOrElseIf, out outerIfOrElseIf))
                return Task.FromResult(false);

            return CanBeMergedAsync(document, syntaxFacts, ifGenerator, outerIfOrElseIf, ifOrElseIf, cancellationToken);
        }

        protected sealed override Task<bool> CanBeMergedDownAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode innerIfStatement)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            if (!IsFirstStatementIfStatement(syntaxFacts, ifGenerator, ifOrElseIf, out innerIfStatement))
                return Task.FromResult(false);

            return CanBeMergedAsync(document, syntaxFacts, ifGenerator, ifOrElseIf, innerIfStatement, cancellationToken);
        }

        protected sealed override SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode outerIfOrElseIf, SyntaxNode innerIfStatement)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            Debug.Assert(syntaxFacts.IsExecutableStatement(innerIfStatement));

            var newCondition = generator.LogicalAndExpression(
                ifGenerator.GetCondition(outerIfOrElseIf),
                ifGenerator.GetCondition(innerIfStatement));

            var newIfOrElseIf = ifGenerator.WithStatementsOf(
                ifGenerator.WithCondition(outerIfOrElseIf, newCondition),
                innerIfStatement);

            return root.ReplaceNode(outerIfOrElseIf, newIfOrElseIf.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private bool IsFirstStatementOfIfOrElseIf(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode statement,
            out SyntaxNode ifOrElseIf)
        {
            // Check whether the statement is a first statement inside an if or else if.
            // If it's inside a block, it has to be the first statement of the block.

            // A statement should always be in a statement container, but we'll do a defensive check anyway so that
            // we don't crash if the helper is missing some cases or there's a new language feature it didn't account for.
            Debug.Assert(syntaxFacts.IsStatementContainer(statement.Parent));
            if (syntaxFacts.IsStatementContainer(statement.Parent))
            {
                var statements = syntaxFacts.GetStatementContainerStatements(statement.Parent);
                if (statements.Count > 0 && statements[0] == statement)
                {
                    var rootStatements = WalkUpScopeBlocks(syntaxFacts, statements);
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

        private bool IsFirstStatementIfStatement(
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode ifOrElseIf,
            out SyntaxNode ifStatement)
        {
            // Check whether the first statement inside an if or else if is an if statement.
            // If the if statement is inside a block, it has to be the first statement of the block.

            // An if or else if should always be a statement container, but we'll do a defensive check anyway.
            Debug.Assert(syntaxFacts.IsStatementContainer(ifOrElseIf));
            if (syntaxFacts.IsStatementContainer(ifOrElseIf))
            {
                var rootStatements = syntaxFacts.GetStatementContainerStatements(ifOrElseIf);

                var statements = WalkDownScopeBlocks(syntaxFacts, rootStatements);
                if (statements.Count > 0 && ifGenerator.IsIfOrElseIf(statements[0]))
                {
                    ifStatement = statements[0];
                    return true;
                }
            }

            ifStatement = null;
            return false;
        }

        private async Task<bool> CanBeMergedAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            IIfLikeStatementGenerator ifGenerator,
            SyntaxNode outerIfOrElseIf,
            SyntaxNode innerIfStatement,
            CancellationToken cancellationToken)
        {
            // We can only merge this with the outer if statement if any inner else-if and else clauses are equal
            // to else-if and else clauses following the outer if statement because we'll be removing the inner ones.
            // Example of what we can merge:
            //    if (a)
            //    {
            //        if (b)
            //            Console.WriteLine();
            //        else
            //            Foo();
            //    }
            //    else
            //    {
            //        Foo();
            //    }
            if (!System.Linq.ImmutableArrayExtensions.SequenceEqual(
                    ifGenerator.GetElseIfAndElseClauses(outerIfOrElseIf),
                    ifGenerator.GetElseIfAndElseClauses(innerIfStatement),
                    (a, b) => IsElseIfOrElseClauseEquivalent(syntaxFacts, ifGenerator, a, b)))
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

                // A statement should always be in a statement container, but we'll do a defensive check anyway so that
                // we don't crash if the helper is missing some cases or there's a new language feature it didn't account for.
                Debug.Assert(syntaxFacts.IsStatementContainer(outerIfStatement.Parent));
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

            var statements1 = WalkDownScopeBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(elseIfOrElseClause1));
            var statements2 = WalkDownScopeBlocks(syntaxFacts, syntaxFacts.GetStatementContainerStatements(elseIfOrElseClause2));

            return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, MergeDirection direction, string ifKeywordText)
                : base(string.Format(GetResourceText(direction), ifKeywordText), createChangedDocument)
            {
            }

            private static string GetResourceText(MergeDirection direction)
                => direction == MergeDirection.Up ? FeaturesResources.Merge_with_outer_0_statement : FeaturesResources.Merge_with_nested_0_statement;
        }
    }
}
