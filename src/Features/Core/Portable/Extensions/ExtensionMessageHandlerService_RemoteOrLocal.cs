// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Extensions;

internal sealed partial class ExtensionMessageHandlerServiceFactory
{
    private sealed partial class ExtensionMessageHandlerService
    {
        // Code for bifurcating calls to either the local or remote process.

        private async ValueTask ExecuteActionInRemoteOrCurrentProcessAsync<TArg>(
            Solution? solution,
            Func<ExtensionMessageHandlerService, TArg, CancellationToken, ValueTask> executeInProcessAsync,
            Func<IRemoteExtensionMessageHandlerService, TArg, Checksum?, CancellationToken, ValueTask> executeOutOfProcessAsync,
            TArg arg,
            CancellationToken cancellationToken)
        {
            await ExecuteFuncInRemoteOrCurrentProcessAsync(
                solution,
                static async (localService, tuple, cancellationToken) =>
                {
                    var (executeInProcessAsync, _, arg) = tuple;
                    await executeInProcessAsync(localService, arg, cancellationToken).ConfigureAwait(false);
                    return default(VoidResult);
                },
                static async (service, tuple, checksum, cancellationToken) =>
                {
                    var (_, executeOutOfProcessAsync, arg) = tuple;
                    await executeOutOfProcessAsync(service, arg, checksum, cancellationToken).ConfigureAwait(false);
                    return default(VoidResult);
                },
                (executeInProcessAsync, executeOutOfProcessAsync, arg),
                cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<TResult> ExecuteFuncInRemoteOrCurrentProcessAsync<TArg, TResult>(
            Solution? solution,
            Func<ExtensionMessageHandlerService, TArg, CancellationToken, ValueTask<TResult>> executeInProcessAsync,
            Func<IRemoteExtensionMessageHandlerService, TArg, Checksum?, CancellationToken, ValueTask<TResult>> executeOutOfProcessAsync,
            TArg arg,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_solutionServices, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return await executeInProcessAsync(this, arg, cancellationToken).ConfigureAwait(false);

            var result = solution is null
                ? await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService, TResult>(
                    (remoteService, cancellationToken) => executeOutOfProcessAsync(remoteService, arg, null, cancellationToken),
                    cancellationToken).ConfigureAwait(false)
                : await client.TryInvokeAsync<IRemoteExtensionMessageHandlerService, TResult>(
                    solution,
                    (remoteService, checksum, cancellationToken) => executeOutOfProcessAsync(remoteService, arg, checksum, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

            // If the remote call succeeded, this will have a valid value in it and can be returned.  If it did not
            // succeed then we will have already shown the user an error message stating there was an issue making
            // the call, and it's fine for this to throw again, unwinding the stack up to the caller.
            return result.Value;
        }

        public ValueTask RegisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken)
            => ExecuteActionInRemoteOrCurrentProcessAsync(
                solution: null,
                static (localService, assemblyFilePath, _) => localService.RegisterExtensionInCurrentProcessAsync(assemblyFilePath),
                static (remoteService, assemblyFilePath, _, cancellationToken) => remoteService.RegisterExtensionAsync(assemblyFilePath, cancellationToken),
                assemblyFilePath,
                cancellationToken);

        public ValueTask UnregisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken)
            => ExecuteActionInRemoteOrCurrentProcessAsync(
                solution: null,
                static (localService, assemblyFilePath, _) => localService.UnregisterExtensionInCurrentProcessAsync(assemblyFilePath),
                static (remoteService, assemblyFilePath, _, cancellationToken) => remoteService.UnregisterExtensionAsync(assemblyFilePath, cancellationToken),
                assemblyFilePath,
                cancellationToken);

        public ValueTask<ExtensionMessageNames> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken)
            => ExecuteFuncInRemoteOrCurrentProcessAsync(
                solution: null,
                static (localService, assemblyFilePath, cancellationToken) => localService.GetExtensionMessageNamesInCurrentProcessAsync(assemblyFilePath, cancellationToken),
                static (remoteService, assemblyFilePath, _, cancellationToken) => remoteService.GetExtensionMessageNamesAsync(assemblyFilePath, cancellationToken),
                assemblyFilePath,
                cancellationToken);

        public ValueTask ResetAsync(CancellationToken cancellationToken)
            => ExecuteActionInRemoteOrCurrentProcessAsync(
                solution: null,
                static (localService, _, _) => localService.ResetInCurrentProcessAsync(),
                static (remoteService, _, _, cancellationToken) => remoteService.ResetAsync(cancellationToken),
                default(VoidResult),
                cancellationToken);

        public ValueTask<ExtensionMessageResult> HandleExtensionWorkspaceMessageAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
            => ExecuteFuncInRemoteOrCurrentProcessAsync(
                solution,
                static (localService, arg, cancellationToken) =>
                {
                    var (solution, messageName, jsonMessage, handlers) = arg;
                    return localService.HandleExtensionMessageInCurrentProcessAsync(
                        executeArgument: solution, isSolution: true, messageName, jsonMessage, handlers, cancellationToken);
                },
                static (remoteService, arg, checksum, cancellationToken) =>
                {
                    var (_, messageName, jsonMessage, _) = arg;
                    return remoteService.HandleExtensionWorkspaceMessageAsync(
                        checksum!.Value, messageName, jsonMessage, cancellationToken);
                },
                (solution, messageName, jsonMessage, _cachedHandlers_useOnlyUnderLock.workspace),
                cancellationToken);

        public ValueTask<ExtensionMessageResult> HandleExtensionDocumentMessageAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
            => ExecuteFuncInRemoteOrCurrentProcessAsync(
                document.Project.Solution,
                static (localService, arg, cancellationToken) =>
                {
                    var (document, messageName, jsonMessage, handlers) = arg;
                    return localService.HandleExtensionMessageInCurrentProcessAsync(
                        executeArgument: document, isSolution: false, messageName, jsonMessage, handlers, cancellationToken);
                },
                static (remoteService, arg, checksum, cancellationToken) =>
                {
                    var (document, messageName, jsonMessage, _) = arg;
                    return remoteService.HandleExtensionDocumentMessageAsync(
                        checksum!.Value, messageName, jsonMessage, document.Id, cancellationToken);
                },
                (document, messageName, jsonMessage, _cachedHandlers_useOnlyUnderLock.document),
                cancellationToken);
    }
}
