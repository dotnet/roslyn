﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
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
                new RegexEmbeddedLanguageFeatures(this, info));
        }

        /// <summary>Escapes <paramref name="text"/> appropriately so it can be inserted into 
        /// <paramref name="token"/>.  For example if inserting `\p{Number}` into a normal C#
        /// string token, the `\` would have to be escaped into `\\`.  However in a verbatim-string
        /// literal (i.e. `@"..."`) it would not have to be escaped.
        /// </summary>
        /// <param name="token">The original string token that <paramref name="text"/> is being
        /// inserted into.</param>
        internal abstract string EscapeText(string text, SyntaxToken token);
    }
}
