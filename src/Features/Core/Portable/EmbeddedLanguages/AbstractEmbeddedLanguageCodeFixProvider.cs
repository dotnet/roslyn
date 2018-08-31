// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    /// <summary>
    /// A CodeFixProvider that hooks up the diagnostics produced by <see
    /// cref="IEmbeddedDiagnosticAnalyzer"/> and to the appropriate <see
    /// cref="IEmbeddedCodeFixProvider"/>.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly Dictionary<string, IEmbeddedCodeFixProvider> _diagnosticIdToCodeFixProvider;

        public override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected AbstractEmbeddedLanguageCodeFixProvider(
            IEmbeddedLanguagesProvider embeddedLanguagesProvider)
        {
            _diagnosticIdToCodeFixProvider = new Dictionary<string, IEmbeddedCodeFixProvider>();

            // Create a mapping from each IEmbeddedCodeFixProvider.FixableDiagnosticIds back to the
            // IEmbeddedCodeFixProvider itself.  That way, when we hear about diagnostics, we know
            // which provider to actually do the fixing.
            foreach (var language in embeddedLanguagesProvider.GetEmbeddedLanguages())
            {
                var codeFixProvider = language.CodeFixProvider;
                if (codeFixProvider != null)
                {
                    foreach (var diagnosticId in codeFixProvider.FixableDiagnosticIds)
                    {
                        // 'Add' is intentional.  We want to throw if multiple fix providers
                        // register for the say diagnostic ID.
                        _diagnosticIdToCodeFixProvider.Add(diagnosticId, codeFixProvider);
                    }
                }
            }

            this.FixableDiagnosticIds = _diagnosticIdToCodeFixProvider.Keys.ToImmutableArray();
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var firstDiagnostic = context.Diagnostics[0];

            if (_diagnosticIdToCodeFixProvider.TryGetValue(firstDiagnostic.Id, out var provider))
            {
                context.RegisterCodeFix(new MyCodeAction(
                    provider.Title,
                    c => FixAsync(context.Document, firstDiagnostic, c)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            foreach (var diagnostic in diagnostics)
            {
                if (_diagnosticIdToCodeFixProvider.TryGetValue(diagnostic.Id, out var provider))
                {
                    // Defer to the underlying IEmbeddedCodeFixProvider to actually fix.
                    provider.Fix(editor, diagnostic, cancellationToken);
                }
            }

            return Task.CompletedTask;
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
