// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    internal class RegularExpressionsOptions
    {
        public static PerLanguageOption2<bool> ReportInvalidRegexPatterns =
            new(
                nameof(RegularExpressionsOptions),
                nameof(ReportInvalidRegexPatterns),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReportInvalidRegexPatterns"));

        public static PerLanguageOption2<bool> HighlightRelatedRegexComponentsUnderCursor =
            new(
                nameof(RegularExpressionsOptions),
                nameof(HighlightRelatedRegexComponentsUnderCursor),
                defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightRelatedRegexComponentsUnderCursor"));
    }

    [ExportSolutionOptionProvider, Shared]
    internal class RegularExpressionsOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RegularExpressionsOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            RegularExpressionsOptions.ReportInvalidRegexPatterns,
            RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor);
    }
}
