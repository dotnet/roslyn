// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class SolutionCrawlerOptions
    {
        /// <summary>
        /// Option to turn configure background analysis scope for the current user.
        /// </summary>
        public static readonly PerLanguageOption2<BackgroundAnalysisScope> BackgroundAnalysisScopeOption = new(
            nameof(SolutionCrawlerOptions), nameof(BackgroundAnalysisScopeOption), defaultValue: BackgroundAnalysisScope.Default,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption"));

        /// <summary>
        /// Option to turn configure background analysis scope for the current solution.
        /// </summary>
        public static readonly Option2<BackgroundAnalysisScope?> SolutionBackgroundAnalysisScopeOption = new(
            nameof(SolutionCrawlerOptions), nameof(SolutionBackgroundAnalysisScopeOption), defaultValue: null);

        /// <summary>
        /// This option is used by TypeScript and F#.
        /// </summary>
        [Obsolete("Currently used by F# - should move to the new option SolutionCrawlerOptions.BackgroundAnalysisScopeOption")]
        internal static readonly PerLanguageOption<bool?> ClosedFileDiagnostic = new(
            "ServiceFeaturesOnOff", "Closed File Diagnostic", defaultValue: null,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Closed File Diagnostic"));

        /// <summary>
        /// Enables forced <see cref="BackgroundAnalysisScope.Minimal"/> scope when low VM is detected to improve performance.
        /// </summary>
        public static bool LowMemoryForcedMinimalBackgroundAnalysis = false;

        public static BackgroundAnalysisScope GetDefaultBackgroundAnalysisScopeFromOptions(OptionSet options, string language)
        {
            switch (language)
            {
                case LanguageNames.FSharp:
#pragma warning disable CS0618 // Type or member is obsolete - F# is still on the older ClosedFileDiagnostic option.
                    var option = options.GetOption(ClosedFileDiagnostic, language);
#pragma warning restore CS0618 // Type or member is obsolete

                    // Note that the default value for this option is 'true' for this language.
                    if (!option.HasValue || option.Value)
                    {
                        return BackgroundAnalysisScope.FullSolution;
                    }

                    return BackgroundAnalysisScope.Default;

                default:
                    return options.GetOption(BackgroundAnalysisScopeOption, language);
            }
        }

        public static BackgroundAnalysisScope GetBackgroundAnalysisScopeFromOptions(OptionSet options, string language)
        {
            if (LowMemoryForcedMinimalBackgroundAnalysis)
            {
                return BackgroundAnalysisScope.Minimal;
            }

            if (options.GetOption(SolutionBackgroundAnalysisScopeOption) is { } scope)
            {
                return scope;
            }

            return GetDefaultBackgroundAnalysisScopeFromOptions(options, language);
        }
    }
}
