// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

internal class JsonFeatureOptions
{
    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns =
        new(nameof(JsonFeatureOptions),
            nameof(ReportInvalidJsonPatterns),
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

    public static PerLanguageOption2<bool> HighlightRelatedJsonComponentsUnderCursor =
        new(nameof(JsonFeatureOptions),
            nameof(HighlightRelatedJsonComponentsUnderCursor),
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor"));

    public static PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
        new(nameof(JsonFeatureOptions),
            nameof(DetectAndOfferEditorFeaturesForProbableJsonStrings),
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
}

[ExportSolutionOptionProvider, Shared]
internal class JsonOptionsProvider : IOptionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public JsonOptionsProvider()
    {
    }

    public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
        JsonFeatureOptions.ReportInvalidJsonPatterns,
        JsonFeatureOptions.HighlightRelatedJsonComponentsUnderCursor,
        JsonFeatureOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings);
}
