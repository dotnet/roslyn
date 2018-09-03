// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonEmbeddedLanguage : IEmbeddedLanguage
    {
        public ISyntaxClassifier Classifier { get; }

        public JsonEmbeddedLanguage(EmbeddedLanguageInfo info)
        {
            Classifier = new JsonEmbeddedClassifier(info);
        }
    }
}
