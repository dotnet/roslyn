// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

[ExportWorkspaceService(typeof(ISolutionCrawlerOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SolutionCrawlerOptionsService(IGlobalOptionService globalOptions) : ISolutionCrawlerOptionsService
{
    public bool EnableDiagnosticsInSourceGeneratedFiles
        => globalOptions.GetOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles);
}
