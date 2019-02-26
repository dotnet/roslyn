// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguageFeaturesProvider : AbstractEmbeddedLanguagesProvider, IEmbeddedLanguageFeaturesProvider
    {
        new public ImmutableArray<IEmbeddedLanguageFeatures> Languages { get; }

        protected AbstractEmbeddedLanguageFeaturesProvider(EmbeddedLanguageInfo info) : base(info)
        {
            // No 'Fallback' language added here.  That's because the Fallback language doesn't
            // support any of the IEmbeddedLanguageFeatures or IEmbeddedLanguageEditorFeatures
            // capabilities.
            Languages = ImmutableArray.Create<IEmbeddedLanguageFeatures>(
                new RegexEmbeddedLanguageFeatures(info),
                new JsonEmbeddedLanguageFeatures(this, info));
        }

        /// <summary>
        /// Helper method used by the VB and C# embedded language <see cref="CodeFixProvider"/>s so they can
        /// add special comments to string literals to convey that language services should light up
        /// for them.
        /// </summary>
        internal abstract void AddComment(
            SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents);
    }
}
