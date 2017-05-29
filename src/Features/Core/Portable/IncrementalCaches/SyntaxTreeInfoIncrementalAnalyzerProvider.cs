// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [ExportIncrementalAnalyzerProvider(nameof(SyntaxTreeInfoIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host, WorkspaceKind.RemoteWorkspace }), Shared]
    internal class SyntaxTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new IncrementalAnalyzer();

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            public override Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!document.SupportsSyntaxTree)
                {
                    // Not a language we can produce indices for (i.e. TypeScript).  Bail immediately.
                    return SpecializedTasks.EmptyTask;
                }

                var workspace = document.Project.Solution.Workspace;

                if (workspace.Kind != WorkspaceKind.Test &&
                    workspace.Kind != WorkspaceKind.RemoteWorkspace)
                {
                    // If we're in the local workspace, only precalculate the index if we're not
                    // using the remote service.
                    if (workspace.IsOutOfProcessEnabled(NavigateToOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed) ||
                        workspace.IsOutOfProcessEnabled(FindUsagesOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed) ||
                        workspace.IsOutOfProcessEnabled(SymbolFinderOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed) ||
                        workspace.IsOutOfProcessEnabled(DocumentHighlightingOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed))
                    {
                        return SpecializedTasks.EmptyTask;
                    }
                }

                return SyntaxTreeIndex.PrecalculateAsync(document, cancellationToken);
            }
        }
    }
}