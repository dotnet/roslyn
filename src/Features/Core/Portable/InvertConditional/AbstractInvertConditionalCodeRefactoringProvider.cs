﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InvertConditional
{
    internal abstract class AbstractInvertConditionalCodeRefactoringProvider<TConditionalExpressionSyntax>
        : CodeRefactoringProvider
        where TConditionalExpressionSyntax : SyntaxNode
    {
        protected abstract bool ShouldOffer(TConditionalExpressionSyntax conditional);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var conditional = await FindConditionalAsync(document, span, cancellationToken).ConfigureAwait(false);

            if (conditional == null || !ShouldOffer(conditional))
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                FeaturesResources.Invert_conditional,
                c => InvertConditionalAsync(document, span, c)),
                conditional.Span);
        }

        private static async Task<TConditionalExpressionSyntax> FindConditionalAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
            => await document.TryGetRelevantNodeAsync<TConditionalExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);

        private static async Task<Document> InvertConditionalAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var conditional = await FindConditionalAsync(document, span, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            editor.Generator.SyntaxFacts.GetPartsOfConditionalExpression(conditional,
                out var condition, out var whenTrue, out var whenFalse);

            editor.ReplaceNode(condition, editor.Generator.Negate(editor.Generator.SyntaxGeneratorInternal, condition, semanticModel, cancellationToken));
            editor.ReplaceNode(whenTrue, whenFalse.WithTriviaFrom(whenTrue));
            editor.ReplaceNode(whenFalse, whenTrue.WithTriviaFrom(whenFalse));

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
