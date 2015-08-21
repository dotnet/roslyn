// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host), Shared]
    internal class SyntaxTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new IncrementalAnalyzer();
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            public override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                return SyntaxTreeInfo.PrecalculateAsync(document, cancellationToken);
            }
        }
    }
}
