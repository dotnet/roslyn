// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageFeaturesProvider : AbstractEmbeddedLanguagesProvider, IEmbeddedLanguageFeaturesProvider
    {
        new public ImmutableArray<IEmbeddedLanguageFeatures> Languages { get; }
         
        protected AbstractEmbeddedLanguageFeaturesProvider(EmbeddedLanguageInfo info) : base(info)
        {
            Languages = ImmutableArray.Create<IEmbeddedLanguageFeatures>(
                new RegexEmbeddedLanguageFeatures(this, info));
        }

        internal abstract string EscapeText(string text, SyntaxToken token);
    }
}
