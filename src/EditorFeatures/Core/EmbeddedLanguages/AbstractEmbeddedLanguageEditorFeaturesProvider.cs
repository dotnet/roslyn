// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
<<<<<<< HEAD
=======
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;
>>>>>>> jsonTests

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
<<<<<<< HEAD
            // No 'Fallback' language added here.  That's because the Fallback language doesn't
            // support any of the IEmbeddedLanguageFeatures or IEmbeddedLanguageEditorFeatures
            // capabilities.
            Languages = ImmutableArray.Create<IEmbeddedLanguageEditorFeatures>(
                new RegexEmbeddedLanguageEditorFeatures(this, info),
                new JsonEmbeddedLanguageEditorFeatures(this, info));
=======
            Languages = ImmutableArray.Create<IEmbeddedLanguage>(
                new DateAndTimeEmbeddedLanguageEditorFeatures(info),
                new RegexEmbeddedLanguageEditorFeatures(this, info),
                new FallbackEmbeddedLanguage(info));
>>>>>>> jsonTests
        }
    }
}
