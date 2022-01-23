// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

<<<<<<< HEAD
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
=======
>>>>>>> jsonTests
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEmbeddedLanguageFeatures : IEmbeddedLanguage
    {
        /// <summary>
        /// A optional highlighter that can highlight spans for an embedded language string.
        /// </summary>
        IDocumentHighlightsService? DocumentHighlightsService { get; }

        /// <summary>
<<<<<<< HEAD
        /// Optional analyzers that produces diagnostics for an embedded language string.
        /// </summary>
        ImmutableArray<AbstractBuiltInCodeStyleDiagnosticAnalyzer> DiagnosticAnalyzers { get; }

        /// <summary>
        /// An optional fix provider that can fix the diagnostics produced by <see
        /// cref="DiagnosticAnalyzers"/>
        /// </summary>
        SyntaxEditorBasedCodeFixProvider CodeFixProvider { get; }

        /// <summary>
        /// An optional completion provider that can provide completion items for this
=======
        /// Completion provider that can provide completion items for this
>>>>>>> jsonTests
        /// specific embedded language.
        /// 
        /// <see cref="AbstractAggregateEmbeddedLanguageCompletionProvider"/> will aggregate all these
        /// individual providers and expose them as one single completion provider to
        /// the rest of Roslyn.
        /// </summary>
        EmbeddedLanguageCompletionProvider CompletionProvider { get; }
    }
}
