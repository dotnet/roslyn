// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeIfStatementsCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifOrElseIf);

        protected abstract CodeAction CreateCodeAction(
            Func<CancellationToken, Task<Document>> createChangedDocument, MergeDirection direction, string ifKeywordText);

        protected abstract Task<bool> CanBeMergedUpAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode upperIfOrElseIf);

        protected abstract Task<bool> CanBeMergedDownAsync(
            Document document, SyntaxNode ifOrElseIf, CancellationToken cancellationToken, out SyntaxNode lowerIfOrElseIf);

        protected abstract SyntaxNode GetChangedRoot(Document document, SyntaxNode root, SyntaxNode upperIfOrElseIf, SyntaxNode lowerIfOrElseIf);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(textSpan, getInnermostNodeForTie: true);

            if (IsApplicableSpan(node, textSpan, out var ifOrElseIf))
            {
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var syntaxKinds = document.GetLanguageService<ISyntaxKindsService>();

                if (await CanBeMergedUpAsync(document, ifOrElseIf, cancellationToken, out var upperIfOrElseIf).ConfigureAwait(false))
                    RegisterRefactoring(MergeDirection.Up, upperIfOrElseIf.Span, ifOrElseIf.Span);

                if (await CanBeMergedDownAsync(document, ifOrElseIf, cancellationToken, out var lowerIfOrElseIf).ConfigureAwait(false))
                    RegisterRefactoring(MergeDirection.Down, ifOrElseIf.Span, lowerIfOrElseIf.Span);

                void RegisterRefactoring(MergeDirection direction, TextSpan upperIfOrElseIfSpan, TextSpan lowerIfOrElseIfSpan)
                {
                    context.RegisterRefactoring(
                        CreateCodeAction(
                            c => RefactorAsync(document, upperIfOrElseIfSpan, lowerIfOrElseIfSpan, c),
                            direction,
                            syntaxFacts.GetText(syntaxKinds.IfKeyword)),
                        new TextSpan(upperIfOrElseIfSpan.Start, lowerIfOrElseIfSpan.End));
                }
            }
        }

        private async Task<Document> RefactorAsync(Document document, TextSpan upperIfOrElseIfSpan, TextSpan lowerIfOrElseIfSpan, CancellationToken cancellationToken)
        {
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var upperIfOrElseIf = FindIfOrElseIf(upperIfOrElseIfSpan, ifGenerator, root);
            var lowerIfOrElseIf = FindIfOrElseIf(lowerIfOrElseIfSpan, ifGenerator, root);

            Debug.Assert(ifGenerator.IsIfOrElseIf(upperIfOrElseIf));
            Debug.Assert(ifGenerator.IsIfOrElseIf(lowerIfOrElseIf));

            var newRoot = GetChangedRoot(document, root, upperIfOrElseIf, lowerIfOrElseIf);
            return document.WithSyntaxRoot(newRoot);

            static SyntaxNode FindIfOrElseIf(TextSpan span, IIfLikeStatementGenerator ifGenerator, SyntaxNode root)
            {
                var innerMatch = root.FindNode(span, getInnermostNodeForTie: true);
                return innerMatch?.FirstAncestorOrSelf<SyntaxNode>(
                    node => ifGenerator.IsIfOrElseIf(node) && node.Span == span);
            }
        }

        protected static IReadOnlyList<SyntaxNode> WalkDownScopeBlocks(
            IBlockFacts blockFacts,
            IReadOnlyList<SyntaxNode> statements)
        {
            // If our statements only contain a single block, walk down the block and any subsequent nested blocks
            // to get the real statements inside.

            while (statements.Count == 1 && blockFacts.IsScopeBlock(statements[0]))
                statements = blockFacts.GetExecutableBlockStatements(statements[0]);

            return statements;
        }

        protected static IReadOnlyList<SyntaxNode> WalkUpScopeBlocks(
            IBlockFactsService blockFacts,
            IReadOnlyList<SyntaxNode> statements)
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
                   blockFacts.IsScopeBlock(parent) &&
                   blockFacts.GetExecutableBlockStatements(parent).Count == statements.Count)
            {
                statements = ImmutableArray.Create(parent);
            }

            return statements;
        }

        protected enum MergeDirection { Up, Down }
    }
}
