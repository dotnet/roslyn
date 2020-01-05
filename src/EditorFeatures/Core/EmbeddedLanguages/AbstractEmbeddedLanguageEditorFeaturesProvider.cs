// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageEditorFeaturesProvider
        : AbstractEmbeddedLanguageFeaturesProvider
    {
        public override ImmutableArray<IEmbeddedLanguage> Languages { get; }

        protected AbstractEmbeddedLanguageEditorFeaturesProvider(EmbeddedLanguageInfo info) : base(info)
        {
            Languages = ImmutableArray.Create<IEmbeddedLanguage>(
                new RegexEmbeddedLanguageEditorFeatures(this, info),
                new FallbackEmbeddedLanguage(info));
        }
    }
}
