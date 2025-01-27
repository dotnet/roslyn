// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Available in workspaces that accept changes in solution level analyzers.
/// </summary>
internal interface ISolutionAnalyzerSetterWorkspaceService : IWorkspaceService
{
    void SetAnalyzerReferences(ImmutableArray<AnalyzerReference> references);
}

internal sealed class DefaultSolutionAnalyzerSetterWorkspaceService(Workspace workspace) : ISolutionAnalyzerSetterWorkspaceService
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionAnalyzerSetterWorkspaceService), ServiceLayer.Default), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class Factory() : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new DefaultSolutionAnalyzerSetterWorkspaceService(workspaceServices.Workspace);
    }

    public void SetAnalyzerReferences(ImmutableArray<AnalyzerReference> references)
        => workspace.SetCurrentSolution(s => s.WithAnalyzerReferences(references), WorkspaceChangeKind.SolutionChanged);
}
