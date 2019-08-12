// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QualifyMemberAccess
{
    internal abstract class AbstractQualifyMemberAccessCodeFixprovider<TSimpleNameSyntax, TInvocationSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TSimpleNameSyntax : SyntaxNode
        where TInvocationSyntax : SyntaxNode
    {
        protected abstract string GetTitle();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.AddQualificationDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                GetTitle(),
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetLanguageService<SyntaxGenerator>();

            foreach (var diagnostic in diagnostics)
            {
                var node = GetNode(diagnostic, cancellationToken);
                if (node != null)
                {
                    var qualifiedAccess =
                        generator.MemberAccessExpression(
                            generator.ThisExpression(),
                            node.WithLeadingTrivia())
                        .WithLeadingTrivia(node.GetLeadingTrivia());

                    editor.ReplaceNode(node, qualifiedAccess);
                }
            }
        }

        protected abstract TSimpleNameSyntax GetNode(Diagnostic diagnostic, CancellationToken cancellationToken);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
