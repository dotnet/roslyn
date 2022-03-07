// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    // Order regex classification before json classification.  Json lights up on probable-json strings, but we don't
    // want that to happen for APIs that are certain to be another language like Regex.
    [ExtensionOrder(Before = PredefinedEmbeddedLanguageClassifierNames.Json)]
    [ExportEmbeddedLanguageClassifierInternal(
        PredefinedEmbeddedLanguageClassifierNames.Regex, LanguageNames.CSharp, supportsUnannotatedAPIs: true, "Regex", "Regexp"), Shared]
    internal class CSharpRegexEmbeddedLanguageClassifier : AbstractRegexEmbeddedLanguageClassifier
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRegexEmbeddedLanguageClassifier()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}
