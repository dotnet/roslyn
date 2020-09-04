// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime
{
    internal partial class DateAndTimeEmbeddedCompletionProvider
    {
        private readonly struct DateAndTimeItem
        {
            public readonly string DisplayText;
            public readonly string InlineDescription;
            public readonly string FullDescription;
            public readonly CompletionChange Change;
            public readonly bool IsDefault;

            public DateAndTimeItem(
                string displayText, string inlineDescription, string fullDescription, CompletionChange change, bool isDefault)
            {
                DisplayText = displayText;
                InlineDescription = inlineDescription;
                FullDescription = fullDescription;
                Change = change;
                IsDefault = isDefault;
            }
        }
    }
}
