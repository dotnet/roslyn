// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageFeaturesProvider : AbstractEmbeddedLanguagesProvider, IEmbeddedLanguageFeaturesProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguageFeatures> _embeddedLanguages;
         
        protected AbstractEmbeddedLanguageFeaturesProvider(EmbeddedLanguageInfo info)
            : base(info)
        {
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguageFeatures>(
                new RegexEmbeddedLanguageFeatures(info));
        }

        public new ImmutableArray<IEmbeddedLanguageFeatures> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }
}
