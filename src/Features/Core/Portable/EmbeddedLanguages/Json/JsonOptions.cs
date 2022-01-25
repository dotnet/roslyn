// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json

internal class JsonOptions
{
    public static PerLanguageOption2<bool> ColorizeJsonPatterns =
        new(
            nameof(JsonOptions),
            nameof(ColorizeJsonPatterns),
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeJsonPatterns"));

    public static PerLanguageOption2<bool> ReportInvalidJsonPatterns =
        new(
            nameof(JsonOptions),
            nameof(ReportInvalidJsonPatterns),
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidJsonPatterns"));

    public static PerLanguageOption2<bool> HighlightRelatedJsonComponentsUnderCursor =
        new(
            nameof(JsonOptions),
            nameof(HighlightRelatedJsonComponentsUnderCursor),
            defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedJsonComponentsUnderCursor"));

    public static PerLanguageOption2<bool> DetectAndOfferEditorFeaturesForProbableJsonStrings =
        new(
            nameof(JsonOptions),
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
        JsonOptions.ColorizeJsonPatterns,
        JsonOptions.ReportInvalidJsonPatterns,
        JsonOptions.HighlightRelatedJsonComponentsUnderCursor,
        JsonOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings);
}
