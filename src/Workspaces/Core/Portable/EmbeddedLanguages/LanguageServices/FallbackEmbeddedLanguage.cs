// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification.Classifiers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// A 'fallback' embedded language that can classify normal escape sequences in 
    /// C# or VB strings if no other embedded languages produce results.
    /// </summary>
    internal partial class FallbackEmbeddedLanguage : IEmbeddedLanguage
    {
        public ISyntaxClassifier Classifier { get; }

        public FallbackEmbeddedLanguage(EmbeddedLanguageInfo info)
            => Classifier = new FallbackSyntaxClassifier(info);
    }
}
