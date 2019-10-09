// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddStaticModifier
{
    internal abstract class AbstractAddStaticModifierCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        protected abstract SyntaxNode MapToDeclarator(SyntaxNode declaration);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected sealed override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var declaration = diagnostic.Location.FindNode(cancellationToken);
                var declarator = MapToDeclarator(declaration);

                var symbol = semanticModel.GetDeclaredSymbol(declarator, cancellationToken);

                editor.ReplaceNode(declaration,
                    (currentDeclaration, generator) => generator.WithModifiers(currentDeclaration, DeclarationModifiers.Static));
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Add_static_modifier, createChangedDocument, nameof(AbstractAddStaticModifierCodeFixProvider))
            {
            }
        }
    }
}
