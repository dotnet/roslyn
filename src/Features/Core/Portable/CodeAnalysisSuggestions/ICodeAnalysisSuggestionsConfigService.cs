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

/// <summary>
/// Service to compute project-wide code analysis diagnostics for first party analyzers/fixes that are relevant
/// for the user and/or the code base to improve the code quality and/or code style of the code base.
/// </summary>
internal interface ICodeAnalysisSuggestionsConfigService : IWorkspaceService
{
    /// <summary>
    /// Compute project-wide code analysis diagnostics for first party analyzers/fixes that are relevant
    /// for the user and/or the code base to improve the code quality and/or code style of the code base.
    /// </summary>
    /// <returns>
    /// Returns an array of tuples '(Category, DiagnosticsById)', where 'Category' is the diagnostic category
    /// of the corresponding diagnostics in the 'DiagnosticsById' map. 'DiagnosticsById' is a dictionary from
    /// diagnostic ID to array of diagnostics with that ID that are reported in the current project.
    /// </returns>
    public Task<ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken);
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
