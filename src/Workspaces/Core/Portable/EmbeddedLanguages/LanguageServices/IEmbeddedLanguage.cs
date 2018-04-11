// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEmbeddedLanguage
    {
        /// <summary>
        /// A optional brace matcher that can match braces in an embedded language string.
        /// </summary>
        IEmbeddedBraceMatcher BraceMatcher { get; }

        /// <summary>
        /// A optional classifier that can produce <see cref="ClassifiedSpan"/>s for an embedded language string.
        /// </summary>
        IEmbeddedClassifier Classifier { get; }

        /// <summary>
        /// A optional highlighter that can highlight spans for an embedded language string.
        /// </summary>
        IEmbeddedHighlighter Highlighter { get; }

        /// <summary>
        /// An optional analyzer that produces diagnostics for an embedded language string.
        /// </summary>
        IEmbeddedDiagnosticAnalyzer DiagnosticAnalyzer { get; }

        /// <summary>
        /// An optional fix provider that can fix the diagnostics produced by <see
        /// cref="DiagnosticAnalyzer"/>
        /// </summary>
        IEmbeddedCodeFixProvider CodeFixProvider { get; }
    }
}
