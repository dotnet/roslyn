// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageEditorFeaturesProvider
        : AbstractEmbeddedLanguageFeaturesProvider, IEmbeddedLanguageEditorFeaturesProvider
    {
        new public ImmutableArray<IEmbeddedLanguageEditorFeatures> Languages { get; }

        protected AbstractEmbeddedLanguageEditorFeaturesProvider(EmbeddedLanguageInfo info) : base(info)
        {
            // No 'Fallback' language added here.  That's because the Fallback language doesn't
            // support any of the IEmbeddedLanguageFeatures or IEmbeddedLanguageEditorFeatures
            // capabilities.
            Languages = ImmutableArray.Create<IEmbeddedLanguageEditorFeatures>(
                new RegexEmbeddedLanguageEditorFeatures(info),
                new JsonEmbeddedLanguageEditorFeatures(this, info));
        }
    }
}
