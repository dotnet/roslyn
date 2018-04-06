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
    internal abstract class AbstractEmbeddedLanguageCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly IEmbeddedLanguageProvider _embeddedLanguageProvider;
        private readonly Dictionary<string, IEmbeddedCodeFixProvider> _diagnosticIdToCodeFixProvider;

        public override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected AbstractEmbeddedLanguageCodeFixProvider(
            IEmbeddedLanguageProvider embeddedLanguageProvider)
        {
            _embeddedLanguageProvider = embeddedLanguageProvider;
            _diagnosticIdToCodeFixProvider = new Dictionary<string, IEmbeddedCodeFixProvider>();

            foreach (var language in embeddedLanguageProvider.GetEmbeddedLanguages())
            {
                var codeFixProvider = language.CodeFixProvider;
                if (codeFixProvider != null)
                {
                    foreach (var diagnosticId in codeFixProvider.FixableDiagnosticIds)
                    {
                        _diagnosticIdToCodeFixProvider[diagnosticId] = codeFixProvider;
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

            return SpecializedTasks.EmptyTask;
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
                    provider.Fix(editor, diagnostic, cancellationToken);
                }
            }

            return SpecializedTasks.EmptyTask;
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
