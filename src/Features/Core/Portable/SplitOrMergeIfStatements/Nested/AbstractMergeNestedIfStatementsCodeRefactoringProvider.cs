// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

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
    {
        var resourceText = direction == MergeDirection.Up ? FeaturesResources.Merge_with_outer_0_statement : FeaturesResources.Merge_with_nested_0_statement;
        var title = string.Format(resourceText, ifKeywordText);
        return CodeAction.Create(title, createChangedDocument, title);
    }

    protected sealed override Task<bool> CanBeMergedUpAsync(
        Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode outerIfOrElseIf)
    {
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        var blockFacts = document.GetLanguageService<IBlockFactsService>();
        var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

        if (!IsFirstStatementOfIfOrElseIf(blockFacts, ifGenerator, ifOrElseIf, out outerIfOrElseIf))
            return SpecializedTasks.False;

        return CanBeMergedAsync(document, syntaxFacts, blockFacts, ifGenerator, outerIfOrElseIf, ifOrElseIf, cancellationToken);
    }

    protected sealed override Task<bool> CanBeMergedDownAsync(
        Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode innerIfStatement)
    {
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        var blockFacts = document.GetLanguageService<IBlockFactsService>();
        var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

        if (!IsFirstStatementIfStatement(blockFacts, ifGenerator, ifOrElseIf, out innerIfStatement))
            return SpecializedTasks.False;

        return CanBeMergedAsync(document, syntaxFacts, blockFacts, ifGenerator, ifOrElseIf, innerIfStatement, cancellationToken);
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

    private static bool IsFirstStatementOfIfOrElseIf(
        IBlockFactsService blockFacts,
        IIfLikeStatementGenerator ifGenerator,
        SyntaxNode statement,
        out SyntaxNode ifOrElseIf)
    {
        // Check whether the statement is a first statement inside an if or else if.
        // If it's inside a block, it has to be the first statement of the block.

        // We can't assume that a statement will always be in a statement container, because an if statement
        // in top level code will be in a GlobalStatement.
        if (blockFacts.IsStatementContainer(statement.Parent))
        {
            var statements = blockFacts.GetStatementContainerStatements(statement.Parent);
            if (statements.Count > 0 && statements[0] == statement)
            {
                var rootStatements = WalkUpScopeBlocks(blockFacts, statements);
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

    private static bool IsFirstStatementIfStatement(
        IBlockFactsService blockFacts,
        IIfLikeStatementGenerator ifGenerator,
        SyntaxNode ifOrElseIf,
        out SyntaxNode ifStatement)
    {
        // Check whether the first statement inside an if or else if is an if statement.
        // If the if statement is inside a block, it has to be the first statement of the block.

        // An if or else if should always be a statement container, but we'll do a defensive check anyway.
        Debug.Assert(blockFacts.IsStatementContainer(ifOrElseIf));
        if (blockFacts.IsStatementContainer(ifOrElseIf))
        {
            var rootStatements = blockFacts.GetStatementContainerStatements(ifOrElseIf);

            var statements = WalkDownScopeBlocks(blockFacts, rootStatements);
            if (statements.Count > 0 && ifGenerator.IsIfOrElseIf(statements[0]))
            {
                ifStatement = statements[0];
                return true;
            }
        }

        ifStatement = null;
        return false;
    }

    private static async Task<bool> CanBeMergedAsync(
        Document document,
        ISyntaxFactsService syntaxFacts,
        IBlockFactsService blockFacts,
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
                (a, b) => IsElseIfOrElseClauseEquivalent(syntaxFacts, blockFacts, ifGenerator, a, b)))
        {
            return false;
        }

        var statements = blockFacts.GetStatementContainerStatements(innerIfStatement.Parent);
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
            Debug.Assert(blockFacts.GetStatementContainer(outerIfStatement) is object);
            if (blockFacts.GetStatementContainer(outerIfStatement) is not { } container)
            {
                return false;
            }

            var outerStatements = blockFacts.GetStatementContainerStatements(container);
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

    private static bool IsElseIfOrElseClauseEquivalent(
        ISyntaxFactsService syntaxFacts,
        IBlockFactsService blockFacts,
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

        var statements1 = WalkDownScopeBlocks(blockFacts, blockFacts.GetStatementContainerStatements(elseIfOrElseClause1));
        var statements2 = WalkDownScopeBlocks(blockFacts, blockFacts.GetStatementContainerStatements(elseIfOrElseClause2));

        return statements1.SequenceEqual(statements2, syntaxFacts.AreEquivalent);
    }
}
