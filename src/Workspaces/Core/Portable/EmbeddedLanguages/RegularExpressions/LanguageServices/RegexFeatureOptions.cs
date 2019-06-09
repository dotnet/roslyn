// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal class RegexFeatureOptions
    {
        public static PerLanguageOption<bool> ColorizeRegexPatterns =
            new PerLanguageOption<bool>(
                nameof(RegexFeatureOptions),
                nameof(ColorizeRegexPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns"));

        public static PerLanguageOption<bool> ReportInvalidRegexPatterns =
            new PerLanguageOption<bool>(
                nameof(RegexFeatureOptions),
                nameof(ReportInvalidRegexPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

        public static PerLanguageOption<bool> HighlightRelatedRegexComponentsUnderCursor =
            new PerLanguageOption<bool>(
                nameof(RegexFeatureOptions),
                nameof(HighlightRelatedRegexComponentsUnderCursor),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor"));

        public static PerLanguageOption<bool> ProvideRegexCompletions =
            new PerLanguageOption<bool>(
                nameof(RegexFeatureOptions),
                nameof(ProvideRegexCompletions),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideRegexCompletions"));
    }

    [ExportOptionProvider, Shared]
    internal class RegexFeatureOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public RegexFeatureOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RegexFeatureOptions.ColorizeRegexPatterns,
            RegexFeatureOptions.ReportInvalidRegexPatterns,
            RegexFeatureOptions.HighlightRelatedRegexComponentsUnderCursor);
    }
}
