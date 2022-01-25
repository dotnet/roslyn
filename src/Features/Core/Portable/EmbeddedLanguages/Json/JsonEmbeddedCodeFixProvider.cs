// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    /// <summary>
    /// Code fix impl for embedded json strings.
    /// </summary>
    internal class JsonEmbeddedCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly AbstractEmbeddedLanguageFeaturesProvider _provider;
        private readonly EmbeddedLanguageInfo _info;

        public JsonEmbeddedCodeFixProvider(
            AbstractEmbeddedLanguageFeaturesProvider provider,
            EmbeddedLanguageInfo info)
        {
            _provider = provider;
            _info = info;
        }

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(JsonDetectionAnalyzer.DiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        public void Fix(SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var stringLiteral = diagnostic.Location.FindToken(cancellationToken);
            Debug.Assert(_info.SyntaxFacts.SyntaxKinds.StringLiteralToken == stringLiteral.RawKind);

            var commentContents = diagnostic.Properties.ContainsKey(JsonDetectionAnalyzer.StrictKey)
                ? "lang=json,strict"
                : "lang=json";

            _provider.AddComment(editor, stringLiteral, commentContents);
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
                Fix(editor, diagnostic, cancellationToken);

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Enable_JSON_editor_features, createChangedDocument, nameof(FeaturesResources.Enable_JSON_editor_features))
            {
            }
        }
    }
}
