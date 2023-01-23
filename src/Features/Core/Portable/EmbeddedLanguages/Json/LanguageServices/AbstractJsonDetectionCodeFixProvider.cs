// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Code fix impl for embedded json strings.
    /// </summary>
    internal abstract class AbstractJsonDetectionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly EmbeddedLanguageInfo _info;

        protected AbstractJsonDetectionCodeFixProvider(
            EmbeddedLanguageInfo info)
        {
            _info = info;
        }

        protected abstract void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AbstractJsonDetectionAnalyzer.DiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    FeaturesResources.Enable_all_JSON_editor_features,
                    GetDocumentUpdater(context),
                    nameof(FeaturesResources.Enable_all_JSON_editor_features)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        public void Fix(SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var stringLiteral = diagnostic.Location.FindToken(cancellationToken);
            Debug.Assert(_info.IsAnyStringLiteral(stringLiteral.RawKind));

            var commentContents = diagnostic.Properties.ContainsKey(AbstractJsonDetectionAnalyzer.StrictKey)
                ? "lang=json,strict"
                : "lang=json";

            this.AddComment(editor, stringLiteral, commentContents);
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
                Fix(editor, diagnostic, cancellationToken);

            return Task.CompletedTask;
        }
    }
}
