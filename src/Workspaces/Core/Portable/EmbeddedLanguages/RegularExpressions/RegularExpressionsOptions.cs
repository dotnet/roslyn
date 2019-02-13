// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    internal class RegularExpressionsOptions
    {
        public static PerLanguageOption<bool> ColorizeRegexPatterns =
            new PerLanguageOption<bool>(
                nameof(RegularExpressionsOptions),
                nameof(ColorizeRegexPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns"));

        public static PerLanguageOption<bool> ReportInvalidRegexPatterns =
            new PerLanguageOption<bool>(
                nameof(RegularExpressionsOptions),
                nameof(ReportInvalidRegexPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

        public static PerLanguageOption<bool> HighlightRelatedRegexComponentsUnderCursor =
            new PerLanguageOption<bool>(
                nameof(RegularExpressionsOptions),
                nameof(HighlightRelatedRegexComponentsUnderCursor),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor"));
    }

    [ExportOptionProvider, Shared]
    internal class RegularExpressionsOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RegularExpressionsOptions.ColorizeRegexPatterns,
            RegularExpressionsOptions.ReportInvalidRegexPatterns,
            RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor);
    }
}
