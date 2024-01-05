﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal partial class RegexEmbeddedCompletionProvider
    {
        private readonly struct RegexItem(
            string displayText, string inlineDescription, string fullDescription, CompletionChange change)
        {
            public readonly string DisplayText = displayText;
            public readonly string InlineDescription = inlineDescription;
            public readonly string FullDescription = fullDescription;
            public readonly CompletionChange Change = change;
        }
    }
}
