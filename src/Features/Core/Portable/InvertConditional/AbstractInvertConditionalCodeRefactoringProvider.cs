// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InvertConditional
{
    internal abstract class AbstractInvertConditionalCodeRefactoringProvider<TConditionalExpressionSyntax>
        : CodeRefactoringProvider
        where TConditionalExpressionSyntax : SyntaxNode
    {
        protected abstract bool ShouldOffer(TConditionalExpressionSyntax conditional, int position);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            if (span.Length > 0)
            {
                return;
            }

            var position = span.Start;
            var conditional = await FindConditionalAsync(document, position, cancellationToken).ConfigureAwait(false);

            if (conditional == null ||
                position < conditional.Span.Start ||
                !ShouldOffer(conditional, position))
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                FeaturesResources.Invert_conditional,
                c => InvertConditionalAsync(document, position, c)));
        }

        private static async Task<TConditionalExpressionSyntax> FindConditionalAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            return token.Parent.FirstAncestorOrSelf<TConditionalExpressionSyntax>();
        }

        private async Task<Document> InvertConditionalAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var conditional = await FindConditionalAsync(document, position, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            syntaxFacts.GetPartsOfConditionalExpression(conditional,
                out var condition, out var whenTrue, out var whenFalse);

            editor.ReplaceNode(condition, editor.Generator.Negate(condition, semanticModel, cancellationToken));
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
