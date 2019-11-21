// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class SolutionCrawlerOptions
    {
        /// <summary>
        /// Option to turn configure background analysis scope.
        /// </summary>
        public static readonly Option<BackgroundAnalysisScope> BackgroundAnalysisScopeOption = new Option<BackgroundAnalysisScope>(
            nameof(SolutionCrawlerOptions), nameof(BackgroundAnalysisScopeOption), defaultValue: BackgroundAnalysisScope.Default,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.BackgroundAnalysisScopeOption"));

        /// <summary>
        /// Enables forced <see cref="BackgroundAnalysisScope.Minimal"/> scope when low VM is detected to improve performance.
        /// </summary>
        public static bool LowMemoryForcedMinimalBackgroundAnalysis = false;

        public static BackgroundAnalysisScope GetBackgroundAnalysisScope(Project project)
            => GetBackgroundAnalysisScope(project.Solution.Options);

        public static BackgroundAnalysisScope GetBackgroundAnalysisScope(OptionSet options)
        {
            if (LowMemoryForcedMinimalBackgroundAnalysis)
            {
                return BackgroundAnalysisScope.Minimal;
            }

            return options.GetOption(BackgroundAnalysisScopeOption);
        }
    }
}
