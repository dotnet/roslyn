// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Provides a compile-time view of the current workspace solution.
    /// Workaround for Razor projects which generate both design-time and compile-time source files.
    /// TODO: remove https://github.com/dotnet/roslyn/issues/51678
    /// </summary>
    internal sealed class CompileTimeSolutionProvider : ICompileTimeSolutionProvider
    {
        [ExportWorkspaceServiceFactory(typeof(ICompileTimeSolutionProvider), WorkspaceKind.Host), Shared]
        private sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
                => new CompileTimeSolutionProvider(workspaceServices.Workspace);
        }

        private const string RazorEncConfigFileName = "RazorSourceGenerator.razorencconfig";

        private readonly Workspace _workspace;

        private readonly object _gate = new();

        /// <summary>
        /// Cached compile time solution corresponding to the <see cref="Workspace.PrimaryBranchId"/>
        /// </summary>
        private (int DesignTimeSolutionVersion, Solution CompileTimeSolution)? _primaryBranchCompileTimeSolutionCache;

        /// <summary>
        /// Cached compile time solution for a forked branch.  This is used primarily by LSP cases where
        /// we fork the workspace solution and request diagnostics for the forked solution.
        /// </summary>
        private (int DesignTimeSolutionVersion, Solution CompileTimeSolution)? _forkedBranchCompileTimeSolutionCache;

        public CompileTimeSolutionProvider(Workspace workspace)
        {
            workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
                        _primaryBranchCompileTimeSolutionCache = null;
                        _forkedBranchCompileTimeSolutionCache = null;
                    }
                }
            };
            _workspace = workspace;
        }

        private static bool IsRazorAnalyzerConfig(TextDocumentState documentState)
            => documentState.FilePath != null && documentState.FilePath.EndsWith(RazorEncConfigFileName, StringComparison.OrdinalIgnoreCase);

        public Solution GetCompileTimeSolution(Solution designTimeSolution)
        {
            lock (_gate)
            {
                var cachedCompileTimeSolution = GetCachedCompileTimeSolution(designTimeSolution);

                // Design time solution hasn't changed since we calculated the last compile-time solution:
                if (cachedCompileTimeSolution != null)
                {
                    return cachedCompileTimeSolution;
                }

                using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var configIdsToRemove);
                using var _2 = ArrayBuilder<DocumentId>.GetInstance(out var documentIdsToRemove);

                foreach (var (_, projectState) in designTimeSolution.State.ProjectStates)
                {
                    var anyConfigs = false;

                    foreach (var (_, configState) in projectState.AnalyzerConfigDocumentStates.States)
                    {
                        if (IsRazorAnalyzerConfig(configState))
                        {
                            configIdsToRemove.Add(configState.Id);
                            anyConfigs = true;
                        }
                    }

                    // only remove design-time only documents when source-generated ones replace them
                    if (anyConfigs)
                    {
                        foreach (var (_, documentState) in projectState.DocumentStates.States)
                        {
                            if (documentState.Attributes.DesignTimeOnly)
                            {
                                documentIdsToRemove.Add(documentState.Id);
                            }
                        }
                    }
                }

                var compileTimeSolution = designTimeSolution
                    .RemoveAnalyzerConfigDocuments(configIdsToRemove.ToImmutable())
                    .RemoveDocuments(documentIdsToRemove.ToImmutable());

                UpdateCachedCompileTimeSolution(designTimeSolution, compileTimeSolution);

                return compileTimeSolution;
            }
        }

        private Solution? GetCachedCompileTimeSolution(Solution designTimeSolution)
        {
            // If the design time solution is for the primary branch, retrieve the last cached solution for it.
            // Otherwise this is a forked solution, so retrieve the last forked compile time solution we calculated.
            var cachedCompileTimeSolution = designTimeSolution.BranchId == _workspace.PrimaryBranchId ? _primaryBranchCompileTimeSolutionCache : _forkedBranchCompileTimeSolutionCache;

            // Verify that the design time solution has not changed since the last calculated compile time solution and that
            // the design time solution branch matches the cached instance.
            if (cachedCompileTimeSolution != null
                    && designTimeSolution.WorkspaceVersion == cachedCompileTimeSolution.Value.DesignTimeSolutionVersion
                    && designTimeSolution.BranchId == cachedCompileTimeSolution.Value.CompileTimeSolution.BranchId)
            {
                return cachedCompileTimeSolution.Value.CompileTimeSolution;
            }

            return null;
        }

        private void UpdateCachedCompileTimeSolution(Solution designTimeSolution, Solution compileTimeSolution)
        {
            if (compileTimeSolution.BranchId == _workspace.PrimaryBranchId)
            {
                _primaryBranchCompileTimeSolutionCache = (designTimeSolution.WorkspaceVersion, compileTimeSolution);
            }
            else
            {
                _forkedBranchCompileTimeSolutionCache = (designTimeSolution.WorkspaceVersion, compileTimeSolution);
            }
        }
    }
}
