// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class BackgroundAnalysisScopeExtensions
{
    public static CompilerDiagnosticsScope ToEquivalentCompilerDiagnosticsScope(this BackgroundAnalysisScope backgroundAnalysisScope)
        => backgroundAnalysisScope switch
        {
            BackgroundAnalysisScope.None => CompilerDiagnosticsScope.None,
            BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics => CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics,
            BackgroundAnalysisScope.OpenFiles => CompilerDiagnosticsScope.OpenFiles,
            BackgroundAnalysisScope.FullSolution => CompilerDiagnosticsScope.FullSolution,
            _ => throw ExceptionUtilities.UnexpectedValue(backgroundAnalysisScope),
        };
}
