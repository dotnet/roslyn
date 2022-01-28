// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.Json
{
    internal class JsonEmbeddedLanguageEditorFeatures : JsonEmbeddedLanguageFeatures, IEmbeddedLanguageEditorFeatures
    {
        public IBraceMatcher BraceMatcher { get; }

        public JsonEmbeddedLanguageEditorFeatures(AbstractEmbeddedLanguageEditorFeaturesProvider provider, EmbeddedLanguageInfo info)
            : base(provider, info)
        {
            BraceMatcher = new JsonEmbeddedBraceMatcher(info);
        }
    }
}
