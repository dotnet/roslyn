// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeIfStatementsCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifOrElseIf);

        protected abstract CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText);

        protected abstract Task<bool> CanBeMergedAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken);

        protected abstract SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode ifOrElseIf);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span, getInnermostNodeForTie: true);

            if (IsApplicableSpan(node, context.Span, out var ifOrElseIf))
            {
                var syntaxFacts = context.Document.GetLanguageService<ISyntaxFactsService>();
                var syntaxKinds = context.Document.GetLanguageService<ISyntaxKindsService>();

                if (await CanBeMergedAsync(context.Document, ifOrElseIf, context.CancellationToken).ConfigureAwait(false))
                {
                    context.RegisterRefactoring(
                        CreateCodeAction(
                            c => RefactorAsync(context.Document, context.Span, c),
                            syntaxFacts.GetText(syntaxKinds.IfKeyword)));
                }
            }
        }

        private async Task<Document> RefactorAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span, getInnermostNodeForTie: true);

            Contract.ThrowIfFalse(IsApplicableSpan(node, span, out var ifOrElseIf));

            var newRoot = GetChangedRoot(document, root, ifOrElseIf);
            return document.WithSyntaxRoot(newRoot);
        }

        protected static IReadOnlyList<SyntaxNode> WalkDownPureBlocks(
            ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            // If our statements only contain a single block, walk down the block and any subsequent nested blocks
            // to get the real statements inside.

            while (statements.Count == 1 && syntaxFacts.IsPureBlock(statements[0]))
            {
                statements = syntaxFacts.GetExecutableBlockStatements(statements[0]);
            }

            return statements;
        }

        protected static IReadOnlyList<SyntaxNode> WalkUpPureBlocks(
            ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            // If our statements are inside a block, walk up the block and any subsequent nested blocks that contain
            // no other statements to get the topmost block. The last check is necessary to make sure we stop
            // walking upwards if there are other statements next to our current block:
            // {
            //     {
            //         <original statements>
            //     }
            //     AnotherStatement();
            // }

            while (statements.Count > 0 && statements[0].Parent is var parent &&
                   syntaxFacts.IsPureBlock(parent) &&
                   syntaxFacts.GetExecutableBlockStatements(parent).Count == statements.Count)
            {
                statements = ImmutableArray.Create(parent);
            }

            return statements;
        }
    }
}
