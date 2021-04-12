﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    internal class RegularExpressionsOptions
    {
        public static PerLanguageOption2<bool> ColorizeRegexPatterns =
            new(
                nameof(RegularExpressionsOptions),
                nameof(ColorizeRegexPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ColorizeRegexPatterns"));

        public static PerLanguageOption2<bool> ReportInvalidRegexPatterns =
            new(
                nameof(RegularExpressionsOptions),
                nameof(ReportInvalidRegexPatterns),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

        public static PerLanguageOption2<bool> HighlightRelatedRegexComponentsUnderCursor =
            new(
                nameof(RegularExpressionsOptions),
                nameof(HighlightRelatedRegexComponentsUnderCursor),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor"));

        public static PerLanguageOption2<bool> ProvideRegexCompletions =
            new(
                nameof(RegularExpressionsOptions),
                nameof(ProvideRegexCompletions),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideRegexCompletions"));
    }

    [ExportOptionProvider, Shared]
    internal class RegularExpressionsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RegularExpressionsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<Options.IOption>(
            RegularExpressionsOptions.ColorizeRegexPatterns,
            RegularExpressionsOptions.ReportInvalidRegexPatterns,
            RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor,
            RegularExpressionsOptions.ProvideRegexCompletions);
    }
}
