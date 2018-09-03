// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal class RegexEmbeddedLanguage : IEmbeddedLanguage
    {
        public ISyntaxClassifier Classifier { get; }

        public RegexEmbeddedLanguage(EmbeddedLanguageInfo info)
        {
            Classifier = new RegexSyntaxClassifier(info);
        }
    }
}
