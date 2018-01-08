// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Json
{
    internal class JsonOptions
    {
        public static PerLanguageOption<bool> ColorizeJsonPatterns =
            new PerLanguageOption<bool>(
                nameof(JsonOptions),
                nameof(ColorizeJsonPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns"));

        public static PerLanguageOption<bool> ReportInvalidJsonPatterns =
            new PerLanguageOption<bool>(
                nameof(JsonOptions), 
                nameof(ReportInvalidJsonPatterns), 
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

        public static PerLanguageOption<bool> HighlightRelatedJsonComponentsUnderCursor =
            new PerLanguageOption<bool>(
                nameof(JsonOptions),
                nameof(HighlightRelatedJsonComponentsUnderCursor),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor"));

        public static PerLanguageOption<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
            new PerLanguageOption<bool>(
                nameof(JsonOptions),
                nameof(DetectAndOfferEditorFeaturesForProbableJsonStrings),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.DetectAndOfferEditorFeaturesForProbableJsonStrings"));
    }

    [ExportOptionProvider, Shared]
    internal class JsonOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            JsonOptions.ColorizeJsonPatterns,
            JsonOptions.ReportInvalidJsonPatterns,
            JsonOptions.HighlightRelatedJsonComponentsUnderCursor,
            JsonOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings);
    }
}
