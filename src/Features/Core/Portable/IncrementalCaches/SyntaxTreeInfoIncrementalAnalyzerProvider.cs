// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [ExportIncrementalAnalyzerProvider(nameof(SyntaxTreeInfoIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host, WorkspaceKind.RemoteWorkspace }), Shared]
    internal class SyntaxTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        public SyntaxTreeInfoIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new IncrementalAnalyzer();
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            public override Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!document.SupportsSyntaxTree)
                {
                    // Not a language we can produce indices for (i.e. TypeScript).  Bail immediately.
                    return Task.CompletedTask;
                }

                if (!RemoteFeatureOptions.ShouldComputeIndex(document.Project.Solution.Workspace))
                {
                    return Task.CompletedTask;
                }

                return SyntaxTreeIndex.PrecalculateAsync(document, cancellationToken);
            }
        }
    }
}
