// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class SolutionCrawlerOptions
    {
        /// <summary>
        /// Option to turn configure background analysis scope.
        /// </summary>
        public static readonly PerLanguageOption<BackgroundAnalysisScope> BackgroundAnalysisScopeOption = new PerLanguageOption<BackgroundAnalysisScope>(
            nameof(SolutionCrawlerOptions), nameof(BackgroundAnalysisScopeOption), defaultValue: BackgroundAnalysisScope.Default,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption"));

        /// <summary>
        /// Enables forced <see cref="BackgroundAnalysisScope.Minimal"/> scope when low VM is detected to improve performance.
        /// </summary>
        public static bool LowMemoryForcedMinimalBackgroundAnalysis = false;

        public static BackgroundAnalysisScope GetBackgroundAnalysisScope(Project project)
            => GetBackgroundAnalysisScope(project.Solution.Options, project.Language);

        public static BackgroundAnalysisScope GetBackgroundAnalysisScope(OptionSet options, string language)
        {
            if (LowMemoryForcedMinimalBackgroundAnalysis)
            {
                return BackgroundAnalysisScope.Minimal;
            }

            switch (language)
            {
                case LanguageNames.CSharp:
                case LanguageNames.VisualBasic:
                    return options.GetOption(BackgroundAnalysisScopeOption, language);

                default:
#pragma warning disable CS0618 // Type or member is obsolete - TypeScript and F# are still on the older ClosedFileDiagnostic option.
                    var option = options.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, language);
#pragma warning restore CS0618 // Type or member is obsolete

                    // Note that the default value for this option is 'true' for these languages.
                    if (!option.HasValue || option.Value)
                    {
                        return BackgroundAnalysisScope.FullSolution;
                    }

                    return BackgroundAnalysisScope.Default;
            }
        }
    }
}
