// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguagesProvider : IEmbeddedLanguagesProvider
    {
        private readonly ImmutableArray<IEmbeddedLanguage> _embeddedLanguages;
         
        protected AbstractEmbeddedLanguagesProvider(EmbeddedLanguageInfo info)
        {
            _embeddedLanguages = ImmutableArray.Create<IEmbeddedLanguage>(
                new RegexEmbeddedLanguage(info),
                new FallbackEmbeddedLanguage(info));
        }

        public ImmutableArray<IEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }
}
