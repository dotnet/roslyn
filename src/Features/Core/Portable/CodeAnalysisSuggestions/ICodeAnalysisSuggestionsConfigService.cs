// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CodeAnalysisSuggestions;

internal interface ICodeAnalysisSuggestionsConfigService : IWorkspaceService
{
    public Task<ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICodeAnalysisSuggestionsConfigService), ServiceLayer.Default), Shared]
internal sealed class DefaultCodeAnalysisSuggestionsConfigService : ICodeAnalysisSuggestionsConfigService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultCodeAnalysisSuggestionsConfigService()
    {
    }

    public Task<ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken)
        => Task.FromResult(ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>.Empty);
}
