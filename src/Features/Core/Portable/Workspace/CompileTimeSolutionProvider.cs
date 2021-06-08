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

        private readonly object _gate = new();

        private Solution? _lazyCompileTimeSolution;
        private int? _correspondingDesignTimeSolutionVersion;

        public CompileTimeSolutionProvider(Workspace workspace)
        {
            workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
                        _lazyCompileTimeSolution = null;
                        _correspondingDesignTimeSolutionVersion = null;
                    }
                }
            };
        }

        private static bool IsRazorAnalyzerConfig(TextDocumentState documentState)
            => documentState.FilePath != null && documentState.FilePath.EndsWith(RazorEncConfigFileName, StringComparison.OrdinalIgnoreCase);

        public Solution GetCompileTimeSolution(Solution designTimeSolution)
        {
            lock (_gate)
            {
                // Design time solution hasn't changed since we calculated the last compile-time solution:
                if (designTimeSolution.WorkspaceVersion == _correspondingDesignTimeSolutionVersion)
                {
                    Contract.ThrowIfNull(_lazyCompileTimeSolution);
                    return _lazyCompileTimeSolution;
                }

                using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var configIdsToRemove);
                using var _2 = ArrayBuilder<DocumentId>.GetInstance(out var documentIdsToRemove);

                var compileTimeSolution = designTimeSolution;

                foreach (var (_, projectState) in designTimeSolution.State.ProjectStates)
                {
                    var anyConfigs = false;

                    foreach (var configState in projectState.AnalyzerConfigDocumentStates.States)
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
                        foreach (var documentState in projectState.DocumentStates.States)
                        {
                            if (documentState.Attributes.DesignTimeOnly)
                            {
                                documentIdsToRemove.Add(documentState.Id);
                            }
                        }
                    }
                }

                _lazyCompileTimeSolution = designTimeSolution
                    .RemoveAnalyzerConfigDocuments(configIdsToRemove.ToImmutable())
                    .RemoveDocuments(documentIdsToRemove.ToImmutable());

                _correspondingDesignTimeSolutionVersion = designTimeSolution.WorkspaceVersion;
                return _lazyCompileTimeSolution;
            }
        }
    }
}
