// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

[ExportWorkspaceService(typeof(ISolutionCrawlerOptionsService)), Shared]
internal sealed class SolutionCrawlerOptionsService : ISolutionCrawlerOptionsService
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SolutionCrawlerOptionsService(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool EnableDiagnosticsInSourceGeneratedFiles
        => _globalOptions.GetOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles) ??
        _globalOptions.GetOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFilesFeatureFlag);
}
