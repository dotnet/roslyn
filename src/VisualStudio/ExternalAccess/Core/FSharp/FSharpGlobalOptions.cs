// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp;

[Export(typeof(FSharpGlobalOptions)), Shared]
internal sealed class FSharpGlobalOptions
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpGlobalOptions(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool BlockForCompletionItems
    {
        get => _globalOptions.GetOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.FSharp);
        set => _globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.FSharp, value);
    }

    public void SetBackgroundAnalysisScope(bool openFilesOnly)
    {
        _globalOptions.SetGlobalOption(
            SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.FSharp,
            openFilesOnly ? BackgroundAnalysisScope.OpenFiles : BackgroundAnalysisScope.FullSolution);
        _globalOptions.SetGlobalOption(
            SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.FSharp,
            openFilesOnly ? CompilerDiagnosticsScope.OpenFiles : CompilerDiagnosticsScope.FullSolution);
    }
}
