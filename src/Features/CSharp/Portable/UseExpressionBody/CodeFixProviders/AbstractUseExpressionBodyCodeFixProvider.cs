// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract partial class AbstractUseExpressionBodyCodeFixProvider<TDeclaration> : 
        SyntaxEditorBasedCodeFixProvider
        where TDeclaration : SyntaxNode
    {
        private readonly AbstractUseExpressionBodyHelper<TDeclaration> _helper;

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected AbstractUseExpressionBodyCodeFixProvider(
            string diagnosticId,
            AbstractUseExpressionBodyHelper<TDeclaration> helper)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
            _helper = helper;
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => diagnostic.Severity != DiagnosticSeverity.Hidden;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var documentOptionSet = await context.Document.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            context.RegisterCodeFix(
                new MyCodeAction(diagnostic.GetMessage(), priority, c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, diagnostic, options, cancellationToken);
            }
        }

        private void AddEdits(
            SyntaxEditor editor, Diagnostic diagnostic,
            OptionSet options, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var declaration = (TDeclaration)declarationLocation.FindNode(cancellationToken);

            var updatedDeclaration = _helper.Update(declaration, options)
                                            .WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(declaration, updatedDeclaration);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(string title, CodeActionPriority priority, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
                this.Priority = priority;
            }
        }
    }
}