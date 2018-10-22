// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeIfStatementsCodeRefactoringProvider<TExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract string IfKeywordText { get; }

        protected abstract bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifStatementNode);

        protected abstract bool IsIfStatement(SyntaxNode node);

        protected abstract CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument);

        protected abstract Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);

        protected abstract SyntaxNode GetChangedRoot(
            SyntaxNode root, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts, SyntaxGenerator generator);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span, getInnermostNodeForTie: true);

            if (IsApplicableSpan(node, context.Span, out var ifStatement))
            {
                var syntaxFacts = context.Document.GetLanguageService<ISyntaxFactsService>();

                if (await CanBeMergedAsync(context.Document, ifStatement, syntaxFacts, context.CancellationToken).ConfigureAwait(false))
                {
                    context.RegisterRefactoring(
                        CreateCodeAction(
                            c => RefactorAsync(context.Document, context.Span, syntaxFacts, c)));
                }
            }
        }

        private async Task<Document> RefactorAsync(Document document, TextSpan span, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span, getInnermostNodeForTie: true);

            Contract.ThrowIfFalse(IsApplicableSpan(node, span, out var ifStatement));

            var newRoot = GetChangedRoot(root, ifStatement, syntaxFacts, document.GetLanguageService<SyntaxGenerator>());
            return document.WithSyntaxRoot(newRoot);
        }

        protected static IReadOnlyList<SyntaxNode> WalkDownBlocks(ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            // If our statements only contain a single block, walk down the block and any subsequent nested blocks
            // to get the real statements inside.

            while (statements.Count == 1 && syntaxFacts.IsPureBlock(statements[0]))
            {
                statements = syntaxFacts.GetExecutableBlockStatements(statements[0]);
            }

            return statements;
        }

        protected static IReadOnlyList<SyntaxNode> WalkUpBlocks(ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            // If our statements are inside a block, walk up the block and any subsequent nested blocks that contain
            // no other statements to get the topmost block.

            while (statements.Count > 0 && syntaxFacts.IsPureBlock(statements[0].Parent) &&
                   syntaxFacts.GetExecutableBlockStatements(statements[0].Parent).Count == statements.Count)
            {
                statements = ImmutableArray.Create(statements[0].Parent);
            }

            return statements;
        }
    }
}
