// Licensed to the .NET Foundation under one or more agreements.
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
        new("dotnet_regular_expressions_options_highlight_related_regex_components_under_cursor",
            defaultValue: true);

    public static PerLanguageOption2<bool> HighlightRelatedJsonComponentsUnderCursor =
        new("dotnet_json_feature_options_highlight_related_json_components_under_cursor",
            defaultValue: HighlightingOptions.Default.HighlightRelatedJsonComponentsUnderCursor);
}
