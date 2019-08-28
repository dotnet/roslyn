// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    [ExportIncrementalAnalyzerProvider(nameof(RemoteSolutionPopulatorProvider), workspaceKinds: new[] { WorkspaceKind.Host }), Shared]
    internal class RemoteSolutionPopulatorProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        public RemoteSolutionPopulatorProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new RemoteSolutionPopulator();
        }

        private class RemoteSolutionPopulator : IncrementalAnalyzerBase
        {
            public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                // this pushes new solution to remote host so that remote host can have new solution get cached before
                // anyone actually asking for it. this is for performance rather than functionality.
                // since remote host such as Roslyn OOP supports pull mode, any missing data will be automatically pulled to
                // remote host when requested if it is not already available in OOP. but by having this, most likely, when
                // a feature requests the solution, the solution already exists in OOP due to this pre-emptively pushing the solution
                // to OOP. if it already exists in OOP, it will become no-op. 
                return solution.Workspace.SynchronizePrimaryWorkspaceAsync(solution, cancellationToken);
            }
        }
    }
}
