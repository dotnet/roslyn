// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    internal class JsonFeatureOptions
    {
        public static PerLanguageOption<bool> ColorizeJsonPatterns =
            new PerLanguageOption<bool>(
                nameof(JsonFeatureOptions),
                nameof(ColorizeJsonPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns"));

        public static PerLanguageOption<bool> ReportInvalidJsonPatterns =
            new PerLanguageOption<bool>(
                nameof(JsonFeatureOptions),
                nameof(ReportInvalidJsonPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

        public static PerLanguageOption<bool> HighlightRelatedJsonComponentsUnderCursor =
            new PerLanguageOption<bool>(
                nameof(JsonFeatureOptions),
                nameof(HighlightRelatedJsonComponentsUnderCursor),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor"));

        public static PerLanguageOption<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
            new PerLanguageOption<bool>(
                nameof(JsonFeatureOptions),
                nameof(DetectAndOfferEditorFeaturesForProbableJsonStrings),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
    }

    [ExportOptionProvider, Shared]
    internal class JsonFeatureOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            JsonFeatureOptions.ColorizeJsonPatterns,
            JsonFeatureOptions.ReportInvalidJsonPatterns,
            JsonFeatureOptions.HighlightRelatedJsonComponentsUnderCursor,
            JsonFeatureOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings);
    }
}
