// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEditorFeaturesEmbeddedLanguage : IFeaturesEmbeddedLanguage
    {
        /// <summary>
        /// A optional brace matcher that can match braces in an embedded language string.
        /// </summary>
        IBraceMatcher BraceMatcher { get; }
    }
}
