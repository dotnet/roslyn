// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Code fix impl for embedded json strings.
    /// </summary>
    internal class JsonEmbeddedCodeFixProvider : IEmbeddedCodeFixProvider
    {
        private readonly AbstractEmbeddedLanguagesProvider _provider;
        private readonly JsonEmbeddedLanguage _language;

        public JsonEmbeddedCodeFixProvider(
            AbstractEmbeddedLanguagesProvider provider,
            JsonEmbeddedLanguage language)
        {
            _provider = provider;
            _language = language;
        }

        public string Title => WorkspacesResources.Enable_JSON_editor_features;

        public ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(JsonDetectionAnalyzer.DiagnosticId);

        public void Fix(SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var stringLiteral = diagnostic.Location.FindToken(cancellationToken);
            Debug.Assert(_language.SyntaxFacts.IsStringLiteral(stringLiteral));

            var commentContents = diagnostic.Properties.ContainsKey(JsonDetectionAnalyzer.StrictKey)
                ? "lang=json,strict"
                : "lang=json";
            
            _provider.AddComment(editor, stringLiteral, commentContents);
        }
    }
}
