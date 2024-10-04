// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

internal static class JsonDetectionOptionsStorage
{
    public static readonly PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings = new(
        "dotnet_unsupported_detect_and_offer_editor_features_for_probable_json_strings",
        defaultValue: true,
        isEditorConfigOption: true);

    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns = new(
        "dotnet_unsupported_report_invalid_json_patterns",
        defaultValue: true,
        isEditorConfigOption: true);

    public static readonly ImmutableArray<IOption2> UnsupportedOptions = [DetectAndOfferEditorFeaturesForProbableJsonStrings, ReportInvalidJsonPatterns];
}
