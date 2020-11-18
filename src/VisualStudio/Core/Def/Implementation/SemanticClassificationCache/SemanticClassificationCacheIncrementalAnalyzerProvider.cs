// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SemanticClassificationCache
{
    [ExportIncrementalAnalyzerProvider(nameof(SemanticClassificationCacheIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host }), Shared]
    internal class SemanticClassificationCacheIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticClassificationCacheIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer? CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (workspace is not VisualStudioWorkspace)
                return null;

            return new SemanticClassificationCacheIncrementalAnalyzer();
        }

        private class SemanticClassificationCacheIncrementalAnalyzer : IncrementalAnalyzerBase
        {
            public override async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                // only process C# and VB.  OOP does not contain files for other languages.
                if (document.Project.Language is not (LanguageNames.CSharp or LanguageNames.VisualBasic))
                    return;

                // Only cache classifications for open files.  This keeps our CPU/memory usage low, but hits the common
                // case of ensuring we cache classifications for the files the user edits so that they're ready the next
                // time they open VS.
                if (!document.IsOpen())
                    return;

                var solution = document.Project.Solution;
                var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    // We don't do anything if we fail to get the external process.  That's the case when something has gone
                    // wrong, or the user is explicitly choosing to run inproc only.   In neither of those cases do we want
                    // to bog down the VS process with the work to semantically classify files.
                    return;
                }

                var statusService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                var isFullyLoaded = await statusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
                Debug.Assert(isFullyLoaded, "We should only be called by the incremental analyzer once the solution is fully loaded.");

                await client.RunRemoteAsync(
                    WellKnownServiceHubService.CodeAnalysis,
                    nameof(IRemoteSemanticClassificationCacheService.CacheSemanticClassificationsAsync),
                    document.Project.Solution,
                    arguments: new object[] { document.Id, isFullyLoaded },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
