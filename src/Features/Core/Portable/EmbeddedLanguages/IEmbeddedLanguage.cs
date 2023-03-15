// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    /// <summary>
    /// Services related to a specific embedded language.
    /// </summary>
    internal interface IEmbeddedLanguage
    {
        /// <summary>
        /// Completion provider that can provide completion items for this
        /// specific embedded language.
        /// 
        /// <see cref="AbstractAggregateEmbeddedLanguageCompletionProvider"/> will aggregate all these
        /// individual providers and expose them as one single completion provider to
        /// the rest of Roslyn.
        /// </summary>
        EmbeddedLanguageCompletionProvider? CompletionProvider { get; }
    }
}
