// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotConfigService : IWorkspaceService
{
    public Task<ImmutableArray<(string, ImmutableArray<DiagnosticDescriptor>)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken);
    public Task<string?> TryGetCodeAnalysisPackageSuggestionConfigDataAsync(Project project, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICopilotConfigService), ServiceLayer.Default), Shared]
internal sealed class DefaultCopilotConfigService : ICopilotConfigService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultCopilotConfigService()
    {
    }

    public Task<ImmutableArray<(string, ImmutableArray<DiagnosticDescriptor>)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken)
        => Task.FromResult(ImmutableArray<(string, ImmutableArray<DiagnosticDescriptor>)>.Empty);

    public Task<string?> TryGetCodeAnalysisPackageSuggestionConfigDataAsync(Project project, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

}
