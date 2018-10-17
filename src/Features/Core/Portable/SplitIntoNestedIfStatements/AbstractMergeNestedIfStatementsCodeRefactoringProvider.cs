// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitIntoNestedIfStatements
{
    internal abstract class AbstractMergeNestedIfStatementsCodeRefactoringProvider<
        TIfStatementSyntax> : CodeRefactoringProvider
        where TIfStatementSyntax : SyntaxNode
    {
        protected abstract string IfKeywordText { get; }

        protected abstract bool IsTokenOfIfStatement(SyntaxToken token, out TIfStatementSyntax ifStatement);

        protected abstract ImmutableArray<SyntaxNode> GetElseClauses(TIfStatementSyntax ifStatement);

        protected abstract TIfStatementSyntax MergeIfStatements(TIfStatementSyntax outerIfStatement, TIfStatementSyntax innerIfStatement, SyntaxNode condition);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            if (context.Span.Length > 0 &&
                context.Span != token.Span)
            {
                return;
            }

            var syntaxFacts = context.Document.GetLanguageService<ISyntaxFactsService>();

            if (IsTokenOfIfStatement(token, out var ifStatement) &&
                IsFirstStatementOfIfStatement(syntaxFacts, ifStatement, out var parentIfStatement) &&
                await CanBeMergedAsync(context.Document, syntaxFacts, parentIfStatement, ifStatement, context.CancellationToken))
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        c => FixAsync(context.Document, context.Span, syntaxFacts, c),
                        IfKeywordText));
            }
        }

        private async Task<Document> FixAsync(Document document, TextSpan span, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            Contract.ThrowIfFalse(IsTokenOfIfStatement(token, out var ifStatement));
            Contract.ThrowIfFalse(IsFirstStatementOfIfStatement(syntaxFacts, ifStatement, out var parentIfStatement));

            var newCondition = document.GetLanguageService<SyntaxGenerator>().LogicalAndExpression(
                syntaxFacts.GetIfStatementCondition(parentIfStatement),
                syntaxFacts.GetIfStatementCondition(ifStatement));

            var newIfStatement = MergeIfStatements(parentIfStatement, ifStatement, newCondition);

            var newRoot = root.ReplaceNode(parentIfStatement, newIfStatement.WithAdditionalAnnotations(Formatter.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }

        private static bool IsFirstStatementOfIfStatement(
            ISyntaxFactsService syntaxFacts, SyntaxNode statement, out TIfStatementSyntax ifStatement)
        {
            if (syntaxFacts.IsStatementContainer(statement.Parent) &&
                syntaxFacts.GetStatementContainerStatements(statement.Parent).FirstOrDefault() == statement)
            {
                do
                {
                    if (statement.Parent is TIfStatementSyntax s)
                    {
                        ifStatement = s;
                        return true;
                    }

                    statement = statement.Parent;
                }
                while (syntaxFacts.IsStatementContainer(statement.Parent) &&
                       syntaxFacts.GetStatementContainerStatements(statement.Parent).TrySingleOrDefault() == statement);
            }

            ifStatement = null;
            return false;
        }

        private async Task<bool> CanBeMergedAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            TIfStatementSyntax outerIfStatement,
            TIfStatementSyntax innerIfStatement,
            CancellationToken cancellationToken)
        {
            if (!GetElseClauses(outerIfStatement).SequenceEqual(GetElseClauses(innerIfStatement), syntaxFacts.AreEquivalent))
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
                // 1. these statements exist below the outer if as well, and
                // 2. control flow can't reach after the end of these statements (otherwise, they would get executed twice).
                // This will typically look like a single return, break, continue or a throw statement.

                if (!syntaxFacts.IsStatementContainer(outerIfStatement.Parent))
                {
                    // This shouldn't happen, but let's be cautious.
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
                var controlFlow = semanticModel.AnalyzeControlFlow(statements.First(), statements.Last());

                return !controlFlow.EndPointIsReachable;
            }
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
