// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractEmbeddedLanguagesProvider : IEmbeddedLanguagesProvider
    {
        public EmbeddedLanguageInfo EmbeddedLanguageInfo { get; }
        public ImmutableArray<IEmbeddedLanguage> Languages { get; }

        protected AbstractEmbeddedLanguagesProvider(EmbeddedLanguageInfo info)
        {
            EmbeddedLanguageInfo = info;
            Languages = ImmutableArray.Create<IEmbeddedLanguage>(
                new DateAndTimeEmbeddedLanguage(info),
                new RegexEmbeddedLanguage(this, info),
                new JsonEmbeddedLanguage());
        }

        /// <summary>Escapes <paramref name="text"/> appropriately so it can be inserted into 
        /// <paramref name="token"/>.  For example if inserting `\p{Number}` into a normal C#
        /// string token, the `\` would have to be escaped into `\\`.  However in a verbatim-string
        /// literal (i.e. `@"..."`) it would not have to be escaped.
        /// </summary>
        /// <param name="token">The original string token that <paramref name="text"/> is being
        /// inserted into.</param>
        public abstract string EscapeText(string text, SyntaxToken token);
    }
}
