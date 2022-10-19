// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class SolutionCrawlerOptionsStorage
    {
        /// <summary>
        /// Option to turn configure background analysis scope for the current user.
        /// </summary>
        public static readonly PerLanguageOption2<BackgroundAnalysisScope> BackgroundAnalysisScopeOption = new(
            "SolutionCrawlerOptionsStorage", "BackgroundAnalysisScopeOption", defaultValue: BackgroundAnalysisScope.Default,
            storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption"));

        /// <summary>
        /// Option to turn configure background analysis scope for the current solution.
        /// </summary>
        public static readonly Option2<BackgroundAnalysisScope?> SolutionBackgroundAnalysisScopeOption = new(
            "SolutionCrawlerOptionsStorage", "SolutionBackgroundAnalysisScopeOption", defaultValue: null);

        public static readonly PerLanguageOption2<bool> RemoveDocumentDiagnosticsOnDocumentClose = new(
            "ServiceFeatureOnOffOptions", "RemoveDocumentDiagnosticsOnDocumentClose", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RemoveDocumentDiagnosticsOnDocumentClose"));

        /// <summary>
        /// Enables forced <see cref="BackgroundAnalysisScope.Minimal"/> scope when low VM is detected to improve performance.
        /// </summary>
        public static bool LowMemoryForcedMinimalBackgroundAnalysis = false;

        /// <summary>
        /// <para>Gets the effective background analysis scope for the current solution.</para>
        ///
        /// <para>Gets the solution-specific analysis scope set through
        /// <see cref="SolutionBackgroundAnalysisScopeOption"/>, or the default analysis scope if no solution-specific
        /// scope is set.</para>
        /// </summary>
        public static BackgroundAnalysisScope GetBackgroundAnalysisScope(this IGlobalOptionService globalOptions, string language)
        {
            if (LowMemoryForcedMinimalBackgroundAnalysis)
            {
                return BackgroundAnalysisScope.Minimal;
            }

            return globalOptions.GetOption(SolutionBackgroundAnalysisScopeOption) ??
                   globalOptions.GetOption(BackgroundAnalysisScopeOption, language);
        }
    }
}
