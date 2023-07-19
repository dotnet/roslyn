// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSMac;

internal static class SolutionCrawlerOptionsAccessor
{
    public static bool LowMemoryForcedMinimalBackgroundAnalysis
    {
        get => SolutionCrawlerOptionsStorage.LowMemoryForcedMinimalBackgroundAnalysis;
        set => SolutionCrawlerOptionsStorage.LowMemoryForcedMinimalBackgroundAnalysis = value;
    }

    public static PerLanguageOption2<BackgroundAnalysisScope> BackgroundAnalysisScopeOption
        => SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption;

    public static BackgroundAnalysisScope GetBackgroundAnalysisScope(IGlobalOptionService globalOptions, string language)
        => SolutionCrawlerOptionsStorage.GetBackgroundAnalysisScope(globalOptions, language);
}
