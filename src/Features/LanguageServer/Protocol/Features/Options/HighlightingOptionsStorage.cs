// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.DocumentHighlighting;

internal static class HighlightingOptionsStorage
{
    public static HighlightingOptions GetHighlightingOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            HighlightRelatedRegexComponentsUnderCursor: globalOptions.GetOption(HighlightRelatedRegexComponentsUnderCursor, language),
            HighlightRelatedJsonComponentsUnderCursor: globalOptions.GetOption(HighlightRelatedJsonComponentsUnderCursor, language));

    public static PerLanguageOption2<bool> HighlightRelatedRegexComponentsUnderCursor =
        new("RegularExpressionsOptions",
            "HighlightRelatedRegexComponentsUnderCursor",
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor"));

    public static PerLanguageOption2<bool> HighlightRelatedJsonComponentsUnderCursor =
        new("JsonFeatureOptions",
            "HighlightRelatedJsonComponentsUnderCursor",
            defaultValue: HighlightingOptions.Default.HighlightRelatedJsonComponentsUnderCursor,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor"));
}
