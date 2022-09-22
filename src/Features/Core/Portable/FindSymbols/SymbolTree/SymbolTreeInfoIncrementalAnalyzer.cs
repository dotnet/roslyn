// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree
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
                var isMethodBodyEdit = bodyOpt != null;

                if (client != null)
                {
                    await client.TryInvokeAsync<IRemoteSymbolFinderService>(
                        document.Project, (service, checksum, cancellationToken) =>
                            service.AnalyzeDocumentAsync(checksum, document.Id, isMethodBodyEdit, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                var service = _workspace.Services.GetRequiredService<SymbolTreeInfoCacheService>();
                await service.AnalyzeDocumentAsync(document, isMethodBodyEdit, cancellationToken).ConfigureAwait(false);
            }

            public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    await client.TryInvokeAsync<IRemoteSymbolFinderService>(
                        project, (service, checksum, cancellationToken) =>
                            service.AnalyzeProjectAsync(checksum, project.Id, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                var service = _workspace.Services.GetRequiredService<SymbolTreeInfoCacheService>();
                await service.AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
            }

            public override async Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    await client.TryInvokeAsync<IRemoteSymbolFinderService>(
                        (service, cancellationToken) => service.RemoveProjectAsync(projectId, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                var service = _workspace.Services.GetRequiredService<SymbolTreeInfoCacheService>();
                service.RemoveProject(projectId);
            }
        }
    }
}
