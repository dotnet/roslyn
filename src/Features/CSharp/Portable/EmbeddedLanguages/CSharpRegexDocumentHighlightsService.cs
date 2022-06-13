// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    [ExtensionOrder(Before = PredefinedEmbeddedLanguageNames.Json)]
    [ExportEmbeddedLanguageDocumentHighlightsService(
        PredefinedEmbeddedLanguageNames.Regex, LanguageNames.CSharp, supportsUnannotatedAPIs: true, "Regex", "Regexp"), Shared]
    internal class CSharpRegexDocumentHighlightsService : AbstractRegexDocumentHighlightsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRegexDocumentHighlightsService()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}
