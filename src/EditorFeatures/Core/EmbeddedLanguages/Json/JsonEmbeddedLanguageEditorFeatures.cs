// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.Json
{
    internal class JsonEmbeddedLanguageEditorFeatures : JsonEmbeddedLanguageFeatures, IEmbeddedLanguageEditorFeatures
    {
        public IBraceMatcher BraceMatcher { get; }

        public JsonEmbeddedLanguageEditorFeatures(
            AbstractEmbeddedLanguageEditorFeaturesProvider provider,
            EmbeddedLanguageInfo info) : base(provider, info)
        {
            BraceMatcher = new JsonEmbeddedBraceMatcher(info);
        }
    }
}
