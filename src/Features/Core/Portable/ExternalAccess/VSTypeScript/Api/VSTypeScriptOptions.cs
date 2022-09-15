// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal static class VSTypeScriptOptions
    {
        public static PerLanguageOption<bool> BlockForCompletionItems { get; } = (PerLanguageOption<bool>)CompletionOptions.BlockForCompletionItems2;

        public static OptionSet WithBackgroundAnalysisScope(this OptionSet options, bool openFilesOnly)
            => options.WithChangedOption(
                    SolutionCrawlerOptions.BackgroundAnalysisScopeOption,
                    InternalLanguageNames.TypeScript,
                    openFilesOnly ? BackgroundAnalysisScope.OpenFilesAndProjects : BackgroundAnalysisScope.FullSolution)
                .WithChangedOption(
                    ServiceFeatureOnOffOptions.RemoveDocumentDiagnosticsOnDocumentClose,
                    InternalLanguageNames.TypeScript,
                    openFilesOnly);
    }
}
