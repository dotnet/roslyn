// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            Classifier = new FallbackSyntaxClassifier(info);
        }
    }
}
