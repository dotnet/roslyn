// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

[Export(typeof(VSTypeScriptGlobalOptions)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptGlobalOptions(IGlobalOptionService globalOptions)
{
    public bool BlockForCompletionItems
    {
        get => Service.GetOption(CompletionViewOptionsStorage.BlockForCompletionItems, InternalLanguageNames.TypeScript);
        set => Service.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, InternalLanguageNames.TypeScript, value);
    }

    public void SetBackgroundAnalysisScope(bool openFilesOnly)
    {
        Service.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript,
            openFilesOnly ? BackgroundAnalysisScope.OpenFiles : BackgroundAnalysisScope.FullSolution);
        Service.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, InternalLanguageNames.TypeScript,
            openFilesOnly ? CompilerDiagnosticsScope.OpenFiles : CompilerDiagnosticsScope.FullSolution);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    [Obsolete("Do not pass workspace")]
    public void SetBackgroundAnalysisScope(Workspace workspace, bool openFilesOnly)
        => SetBackgroundAnalysisScope(openFilesOnly);
#pragma warning restore

    internal IGlobalOptionService Service { get; } = globalOptions;
}
