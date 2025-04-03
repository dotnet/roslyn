// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Extensions;

internal sealed partial class ExtensionMessageHandlerServiceFactory
{
    private sealed partial class ExtensionMessageHandlerService
    {
        // Code for bifurcating calls to either the local or remote process.

        private async ValueTask<TResult> ExecuteInRemoteOrCurrentProcessAsync<TResult>(
            Solution? solution,
            Func<CancellationToken, ValueTask<TResult>> executeInProcessAsync,
            Func<IRemoteExtensionMessageHandlerService, Checksum?, CancellationToken, ValueTask<TResult>> executeOutOfProcessAsync,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(this.SolutionServices, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return await executeInProcessAsync(cancellationToken).ConfigureAwait(false);

            if (solution is null)
            {
                var result = await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService, TResult>(
                    (remoteService, cancellationToken) => executeOutOfProcessAsync(remoteService, null, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                // If the remote call succeeded, this will have a valid valid in it and can be returned.  If it did not
                // succeed then we will have already shown the user an error message stating there was an issue making the call,
                // and it's fine for this to throw again, unwinding the stack up to the caller.
                return result.Value;
            }
            else
            {
                var result = await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService, TResult>(
                    solution,
                    (remoteService, checksum, cancellationToken) => executeOutOfProcessAsync(remoteService, checksum, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
                return result.Value;
            }
        }

        public async ValueTask RegisterExtensionAsync(
            string assemblyFilePath,
            CancellationToken cancellationToken)
        {
            await ExecuteInRemoteOrCurrentProcessAsync(
                solution: null,
                _ => RegisterExtensionInCurrentProcessAsync(assemblyFilePath),
                (remoteService, _, cancellationToken) => remoteService.RegisterExtensionAsync(assemblyFilePath, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask UnregisterExtensionAsync(
            string assemblyFilePath,
            CancellationToken cancellationToken)
        {
            await ExecuteInRemoteOrCurrentProcessAsync(
                solution: null,
                _ => UnregisterExtensionInCurrentProcessAsync(assemblyFilePath),
                (remoteService, _, cancellationToken) => remoteService.UnregisterExtensionAsync(assemblyFilePath, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken)
            => ExecuteInRemoteOrCurrentProcessAsync(
                solution: null,
                cancellationToken => GetExtensionMessageNamesInCurrentProcessAsync(assemblyFilePath, cancellationToken),
                (remoteService, _, cancellationToken) => remoteService.GetExtensionMessageNamesAsync(assemblyFilePath, cancellationToken),
                cancellationToken);

        public async ValueTask ResetAsync(CancellationToken cancellationToken)
        {
            await ExecuteInRemoteOrCurrentProcessAsync(
                solution: null,
                _ => ResetInCurrentProcessAsync(),
                (remoteService, _, cancellationToken) => remoteService.ResetAsync(cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public ValueTask<string> HandleExtensionWorkspaceMessageAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
            => ExecuteInRemoteOrCurrentProcessAsync(
                solution,
                cancellationToken => HandleExtensionMessageInCurrentProcessAsync(
                    executeArgument: solution, isSolution: true, messageName, jsonMessage, _cachedWorkspaceHandlers_useOnlyUnderLock, cancellationToken),
                (remoteService, checksum, cancellationToken) => remoteService.HandleExtensionWorkspaceMessageAsync(checksum!.Value, messageName, jsonMessage, cancellationToken),
                cancellationToken);

        public ValueTask<string> HandleExtensionDocumentMessageAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
            => ExecuteInRemoteOrCurrentProcessAsync(
                document.Project.Solution,
                cancellationToken => HandleExtensionMessageInCurrentProcessAsync(
                    executeArgument: document, isSolution: false, messageName, jsonMessage, _cachedDocumentHandlers_useOnlyUnderLock, cancellationToken),
                (remoteService, checksum, cancellationToken) => remoteService.HandleExtensionDocumentMessageAsync(checksum!.Value, messageName, jsonMessage, document.Id, cancellationToken),
                cancellationToken);
    }
}
#endif
