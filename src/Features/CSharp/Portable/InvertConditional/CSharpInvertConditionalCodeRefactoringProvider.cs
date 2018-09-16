// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.InvertConditional
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpInvertConditionalCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            if (span.Length > 0)
            {
                return;
            }

            var position = span.Start;
            var conditional = await FindConditionalAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (conditional == null)
            {
                return;
            }

            if (position < conditional.Span.Start || position > conditional.QuestionToken.Span.End)
            {
                return;
            }

            if (conditional.ColonToken.IsMissing)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                FeaturesResources.Invert_conditional,
                c => InvertConditionalAsync(document, position, c)));
        }

        private static async Task<ConditionalExpressionSyntax> FindConditionalAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            return token.Parent.FirstAncestorOrSelf<ConditionalExpressionSyntax>();
        }

        private async Task<Document> InvertConditionalAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var conditional = await FindConditionalAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;

            var condition = conditional.Condition;
            var whenTrue = conditional.WhenTrue;
            var whenFalse = conditional.WhenFalse;
            editor.ReplaceNode(
                condition, generator.Negate(condition, semanticModel, cancellationToken));

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
