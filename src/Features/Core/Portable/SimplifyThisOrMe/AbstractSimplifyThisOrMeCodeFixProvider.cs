// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SimplifyThisOrMe
{
    internal abstract partial class AbstractSimplifyThisOrMeCodeFixProvider<
        TMemberAccessExpressionSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TMemberAccessExpressionSyntax : SyntaxNode
    {
        protected AbstractSimplifyThisOrMeCodeFixProvider()
        {
        }

        protected abstract string GetTitle();

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveQualificationDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics[0];

            context.RegisterCodeFix(new MyCodeAction(
                GetTitle(),
                c => FixAsync(document, diagnostic, c),
                IDEDiagnosticIds.RemoveQualificationDiagnosticId), context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var memberAccessNodes = diagnostics.Select(
                d => (TMemberAccessExpressionSyntax)d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken)).ToSet();

            var newRoot = Rewrite(semanticModel, root, memberAccessNodes);
            editor.ReplaceNode(root, newRoot);
        }

        protected abstract SyntaxNode Rewrite(
            SemanticModel semanticModel, SyntaxNode root, ISet<TMemberAccessExpressionSyntax> memberAccessNodes);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
