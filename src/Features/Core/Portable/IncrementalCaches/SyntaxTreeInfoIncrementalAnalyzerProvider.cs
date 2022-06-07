// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [ExportIncrementalAnalyzerProvider(nameof(SyntaxTreeInfoIncrementalAnalyzerProvider), new[] { WorkspaceKind.RemoteWorkspace }), Shared]
    internal class SyntaxTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SyntaxTreeInfoIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new IncrementalAnalyzer();

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            public override async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!document.SupportsSyntaxTree)
                {
                    // Not a language we can produce indices for (i.e. TypeScript).  Bail immediately.
                    return;
                }

                await SyntaxTreeIndex.PrecalculateAsync(document, cancellationToken).ConfigureAwait(false);
                await TopLevelSyntaxTreeIndex.PrecalculateAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
