// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

internal sealed partial class ExtensionMessageHandlerServiceFactory
{
    private sealed partial class ExtensionMessageHandlerService(
        SolutionServices solutionServices,
        IExtensionMessageHandlerFactory customMessageHandlerFactory)
        : IExtensionMessageHandlerService
    {
        private static readonly ConditionalWeakTable<IExtensionMessageHandlerWrapper, IExtensionMessageHandlerWrapper> s_disabledExtensionHandlers = new();

        private readonly SolutionServices _solutionServices = solutionServices;
        private readonly IExtensionMessageHandlerFactory _customMessageHandlerFactory = customMessageHandlerFactory;

        /// <summary>
        /// Lock for <see cref="_folderPathToExtensionFolder_useOnlyUnderLock"/>, <see cref="_cachedDocumentHandlers_useOnlyUnderLock"/>, and <see
        /// cref="_cachedWorkspaceHandlers_useOnlyUnderLock"/>.  Note: this type is designed such that all time while this lock is held
        /// should be minimal.  Importantly, no async work or IO should be done while holding this lock.  Instead,
        /// all of that work should be pushed into AsyncLazy values that compute when asked, outside of this lock.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Extensions assembly load contexts and loaded handlers, indexed by extension folder path.
        /// </summary>
        private readonly Dictionary<string, ExtensionFolder> _folderPathToExtensionFolder_useOnlyUnderLock = new();

        /// <summary>
        /// Cached handlers of document-related messages, indexed by handler message name.
        /// </summary>
        private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>> _cachedDocumentHandlers_useOnlyUnderLock = new();

        /// <summary>
        /// Cached handlers of non-document-related messages, indexed by handler message name.
        /// </summary>
        private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>> _cachedWorkspaceHandlers_useOnlyUnderLock = new();

        private static string GetAssemblyFolderPath(string assemblyFilePath)
        {
            return Path.GetDirectoryName(assemblyFilePath)
                ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");
        }

        private async ValueTask<TResult> ExecuteInRemoteOrCurrentProcessAsync<TResult>(
            Solution? solution,
            Func<CancellationToken, ValueTask<TResult>> executeInProcessAsync,
            Func<IRemoteExtensionMessageHandlerService, Checksum?, CancellationToken, ValueTask<TResult>> executeOutOfProcessAsync,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_solutionServices, cancellationToken).ConfigureAwait(false);
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

        private void ClearCachedHandlers_WhileUnderLock()
        {
            Contract.ThrowIfTrue(!Monitor.IsEntered(_gate));
            _cachedWorkspaceHandlers_useOnlyUnderLock.Clear();
            _cachedDocumentHandlers_useOnlyUnderLock.Clear();
        }

        #region RegisterExtension

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

        public ValueTask<VoidResult> RegisterExtensionInCurrentProcessAsync(string assemblyFilePath)
        {
            var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

            lock (_gate)
            {
                var extensionFolder = _folderPathToExtensionFolder_useOnlyUnderLock.GetOrAdd(
                    assemblyFolderPath,
                    assemblyFolderPath => new ExtensionFolder(this, assemblyFolderPath));

                extensionFolder.RegisterAssembly(assemblyFilePath);

                // After registering, clear out the cached handler names.  They will be recomputed the next time we need them.
                ClearCachedHandlers_WhileUnderLock();
                return default;
            }
        }

        #endregion

        #region UnregisterExtension

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

        private ValueTask<VoidResult> UnregisterExtensionInCurrentProcessAsync(string assemblyFilePath)
        {
            var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

            // Note: unregistering is slightly expensive as we do everything under a lock, to ensure that we have a
            // consistent view of the world.  This is fine as we don't expect this to be called very often.
            lock (_gate)
            {
                if (!_folderPathToExtensionFolder_useOnlyUnderLock.TryGetValue(assemblyFolderPath, out var extensionFolder))
                    throw new InvalidOperationException($"No extension registered as '{assemblyFolderPath}'");

                // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within
                // this folder, we can remove the registration for the extension entirely.
                if (extensionFolder.UnregisterAssembly(assemblyFilePath))
                    _folderPathToExtensionFolder_useOnlyUnderLock.Remove(assemblyFolderPath);

                // After unregistering, clear out the cached handler names.  They will be recomputed the next time we need them.
                ClearCachedHandlers_WhileUnderLock();
                return default;
            }
        }

        #endregion

        #region GetExtensionMessageNames

        public async ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesAsync(
            string assemblyFilePath,
            CancellationToken cancellationToken)
        {
            return await ExecuteInRemoteOrCurrentProcessAsync(
                solution: null,
                cancellationToken => GetExtensionMessageNamesInCurrentProcessAsync(assemblyFilePath, cancellationToken),
                (remoteService, _, cancellationToken) => remoteService.GetExtensionMessageNamesAsync(assemblyFilePath, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesInCurrentProcessAsync(
            string assemblyFilePath,
            CancellationToken cancellationToken)
        {
            var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

            ExtensionFolder? extensionFolder;
            lock (_gate)
            {
                if (!_folderPathToExtensionFolder_useOnlyUnderLock.TryGetValue(assemblyFolderPath, out extensionFolder))
                    throw new InvalidOperationException($"No extensions registered at '{assemblyFolderPath}'");
            }

            var assemblyHandlers = await extensionFolder.GetAssemblyHandlersAsync(assemblyFilePath, cancellationToken).ConfigureAwait(false);

            return new(
                [.. assemblyHandlers.WorkspaceMessageHandlers.Keys],
                [.. assemblyHandlers.DocumentMessageHandlers.Keys]);
        }

        #endregion

        #region Reset

        public async ValueTask ResetAsync(CancellationToken cancellationToken)
        {
            await ExecuteInRemoteOrCurrentProcessAsync(
                solution: null,
                _ => ResetInCurrentProcessAsync(),
                (remoteService, _, cancellationToken) => remoteService.ResetAsync(cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        private ValueTask<VoidResult> ResetInCurrentProcessAsync()
        {
            lock (_gate)
            {
                _folderPathToExtensionFolder_useOnlyUnderLock.Clear();
                ClearCachedHandlers_WhileUnderLock();
                return default;
            }
        }

        #endregion

        #region HandleExtensionMessage

        public async ValueTask<string> HandleExtensionWorkspaceMessageAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
        {
            return await ExecuteInRemoteOrCurrentProcessAsync(
                solution,
                cancellationToken => HandleExtensionMessageInCurrentProcessAsync(
                    executeArgument: solution, isSolution: true, messageName, jsonMessage, _cachedWorkspaceHandlers_useOnlyUnderLock, cancellationToken),
                (remoteService, checksum, cancellationToken) => remoteService.HandleExtensionWorkspaceMessageAsync(checksum!.Value, messageName, jsonMessage, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<string> HandleExtensionDocumentMessageAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
        {
            return await ExecuteInRemoteOrCurrentProcessAsync(
                document.Project.Solution,
                cancellationToken => HandleExtensionMessageInCurrentProcessAsync(
                    executeArgument: document, isSolution: false, messageName, jsonMessage, _cachedDocumentHandlers_useOnlyUnderLock, cancellationToken),
                (remoteService, checksum, cancellationToken) => remoteService.HandleExtensionDocumentMessageAsync(checksum!.Value, messageName, jsonMessage, document.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<string> HandleExtensionMessageInCurrentProcessAsync<TArgument>(
            TArgument executeArgument, bool isSolution, string messageName, string jsonMessage,
            Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<TArgument>>>> cachedHandlers,
            CancellationToken cancellationToken)
        {
            AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<TArgument>>> lazyHandlers;
            lock (_gate)
            {
                // May be called a lot.  So we use the non-allocating form of this lookup pattern.
                lazyHandlers = cachedHandlers.GetOrAdd(
                    messageName,
                    static (messageName, arg) => AsyncLazy.Create(
                        static (arg, cancellationToken) => arg.@this.ComputeHandlersAsync<TArgument>(arg.messageName, arg.isSolution, cancellationToken),
                        (messageName, arg.@this, arg.isSolution)),
                    (messageName, @this: this, isSolution));
            }

            var handlers = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (handlers.Length == 0)
                throw new InvalidOperationException($"No handler found for message {messageName}.");

            if (handlers.Length > 1)
                throw new InvalidOperationException($"Multiple handlers found for message {messageName}.");

            var handler = handlers[0];
            if (s_disabledExtensionHandlers.TryGetValue(handler, out _))
                throw new InvalidOperationException($"Handler was disabled due to previous exception.");

            try
            {
                var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
                var result = await handler.ExecuteAsync(message, executeArgument, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(result, handler.ResponseType);
            }
            catch (Exception ex) when (DisableHandlerAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable();
            }

            bool DisableHandlerAndPropagate(Exception ex)
            {
                FatalError.ReportNonFatalError(ex, ErrorSeverity.Critical);

                // Any exception thrown in this method is left to bubble up to the extension. But we unregister this handler
                // from that assembly to minimize the impact.
                s_disabledExtensionHandlers.TryAdd(handler, handler);
                return false;
            }
        }

        private async Task<ImmutableArray<IExtensionMessageHandlerWrapper<TResult>>> ComputeHandlersAsync<TResult>(
            string messageName, bool isSolution, CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<ExtensionFolder>.GetInstance(out var extensionFolders);
            lock (_gate)
            {
                foreach (var (_, extensionFolder) in _folderPathToExtensionFolder_useOnlyUnderLock)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    extensionFolders.Add(extensionFolder);
                }
            }

            using var _ = ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>>.GetInstance(out var result);
            foreach (var extensionFolder in extensionFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await extensionFolder.AddHandlersAsync(messageName, isSolution, result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutable();
        }

        #endregion
    }
}
#endif
