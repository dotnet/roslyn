// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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
            Languages = ImmutableArray.Create<IEmbeddedLanguage>(new FallbackEmbeddedLanguage(info));
        }
    }
}
