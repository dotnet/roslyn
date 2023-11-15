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
    /// <param name="project">Project to compute diagnostics.</param>
    /// <param name="isExplicitlyInvoked">
    /// Indicates if this method call is from an explicit user invocation or not.
    /// If explicitly invoked by the user, then this API will force compute all the project-wide diagnostics.
    /// Otherwise, it will only operate on the already computed and cached diagnostics from background analysis.
    /// </param>
    /// <returns>
    /// Returns an array of tuples '(Category, Diagnostics)', where 'Category' is the diagnostic category
    /// of the corresponding diagnostics in the 'Diagnostics' array.
    /// </returns>
    public Task<ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, bool isExplicitlyInvoked, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICodeAnalysisSuggestionsConfigService), ServiceLayer.Default), Shared]
internal sealed class DefaultCodeAnalysisSuggestionsConfigService : ICodeAnalysisSuggestionsConfigService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultCodeAnalysisSuggestionsConfigService()
    {
    }

    public Task<ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, bool isExplicitlyInvoked, CancellationToken cancellationToken)
        => Task.FromResult(ImmutableArray<(string, ImmutableArray<DiagnosticData>)>.Empty);
}
