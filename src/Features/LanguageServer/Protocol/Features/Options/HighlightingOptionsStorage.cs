﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.DocumentHighlighting;

internal static class HighlightingOptionsStorage
{
    public static HighlightingOptions GetHighlightingOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            HighlightRelatedRegexComponentsUnderCursor = globalOptions.GetOption(HighlightRelatedRegexComponentsUnderCursor, language),
            HighlightRelatedJsonComponentsUnderCursor = globalOptions.GetOption(HighlightRelatedJsonComponentsUnderCursor, language)
        };

    public static PerLanguageOption2<bool> HighlightRelatedRegexComponentsUnderCursor =
        new("RegularExpressionsOptions_HighlightRelatedRegexComponentsUnderCursor",
            defaultValue: true);

    public static PerLanguageOption2<bool> HighlightRelatedJsonComponentsUnderCursor =
        new("JsonFeatureOptions_HighlightRelatedJsonComponentsUnderCursor",
            defaultValue: HighlightingOptions.Default.HighlightRelatedJsonComponentsUnderCursor);
}
