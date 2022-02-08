// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.RegularExpressions
{
    internal sealed class RegexEmbeddedLanguageEditorFeatures : RegexEmbeddedLanguage, IEmbeddedLanguageEditorFeatures
    {
        public IBraceMatcher BraceMatcher { get; }

        public RegexEmbeddedLanguageEditorFeatures(
            AbstractEmbeddedLanguageFeaturesProvider provider, EmbeddedLanguageInfo info)
            : base(provider, info)
        {
            BraceMatcher = new RegexBraceMatcher(this);
        }
    }
}
