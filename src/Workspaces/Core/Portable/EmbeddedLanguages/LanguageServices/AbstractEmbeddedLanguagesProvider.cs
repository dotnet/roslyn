﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguagesProvider : IEmbeddedLanguagesProvider
    {
        public virtual ImmutableArray<IEmbeddedLanguage> Languages { get; }

        protected AbstractEmbeddedLanguagesProvider(EmbeddedLanguageInfo info)
        {
            Languages = ImmutableArray.Create<IEmbeddedLanguage>(
                new RegexEmbeddedLanguage(info),
                new FallbackEmbeddedLanguage(info));
        }
    }
}
