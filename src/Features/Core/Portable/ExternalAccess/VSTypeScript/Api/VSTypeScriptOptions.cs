// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptOptions
    {
        [Obsolete("Use VSTypeScriptGlobalOptions.SetBackgroundAnalysisScope instead")]
        public static OptionSet WithBackgroundAnalysisScope(this OptionSet options, bool openFilesOnly)
            => options.WithChangedOption(
                    SolutionCrawlerOptions.BackgroundAnalysisScopeOption,
                    InternalLanguageNames.TypeScript,
                    openFilesOnly ? BackgroundAnalysisScope.OpenFiles : BackgroundAnalysisScope.FullSolution)
                .WithChangedOption(
                    ServiceFeatureOnOffOptions.RemoveDocumentDiagnosticsOnDocumentClose,
                    InternalLanguageNames.TypeScript,
                    openFilesOnly);
    }
}
