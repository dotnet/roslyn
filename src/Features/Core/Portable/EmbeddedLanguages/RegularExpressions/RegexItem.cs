// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    internal partial class RegexEmbeddedCompletionProvider
    {
        private readonly struct RegexItem
        {
            public readonly string DisplayText;
            public readonly string InlineDescription;
            public readonly string FullDescription;
            public readonly CompletionChange Change;

            public RegexItem(
                string displayText, string inlineDescription, string fullDescription, CompletionChange change)
            {
                DisplayText = displayText;
                InlineDescription = inlineDescription;
                FullDescription = fullDescription;
                Change = change;
            }
        }
    }
}
