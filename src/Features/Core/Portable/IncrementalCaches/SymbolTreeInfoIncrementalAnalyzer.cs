// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    internal partial class SymbolTreeInfoIncrementalAnalyzerProvider
    {
        private class SymbolTreeInfoIncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private readonly Workspace _workspace;

            public SymbolTreeInfoIncrementalAnalyzer(Workspace workspace)
                => _workspace = workspace;

            public override async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (client is null)
                    return;

                var isMethodBodyEdit = bodyOpt != null;
                await client.TryInvokeAsync<IRemoteSymbolTreeInfoIncrementalAnalyzer>(
                    document.Project, (service, checksum, cancellationToken) =>
                        service.AnalyzeDocumentAsync(checksum, document.Id, isMethodBodyEdit, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }

            public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client is null)
                    return;

                await client.TryInvokeAsync<IRemoteSymbolTreeInfoIncrementalAnalyzer>(
                    project, (service, checksum, cancellationToken) =>
                        service.AnalyzeProjectAsync(checksum, project.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }

            public override async Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client is null)
                    return;

                await client.TryInvokeAsync<IRemoteSymbolTreeInfoIncrementalAnalyzer>(
                    (service, cancellationToken) => service.RemoveProjectAsync(projectId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
