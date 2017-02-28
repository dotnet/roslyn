// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [ExportIncrementalAnalyzerProvider(nameof(SyntaxTreeInfoIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host, WorkspaceKind.RemoteWorkspace }), Shared]
    internal class SyntaxTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new IncrementalAnalyzer();
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            public override Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (document.Project.Solution.Workspace.Options.GetOption(SymbolFinderOptions.OutOfProcessAllowed))
                {
                    return SpecializedTasks.EmptyTask;
                }

                return SyntaxTreeIndex.PrecalculateAsync(document, cancellationToken);
            }
        }
    }
}