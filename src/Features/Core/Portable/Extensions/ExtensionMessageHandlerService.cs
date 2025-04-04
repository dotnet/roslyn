// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

internal sealed partial class ExtensionMessageHandlerServiceFactory
{
    private readonly record struct AssemblyMessageHandlers(
        ImmutableDictionary<string, IExtensionMessageHandlerWrapper> DocumentMessageHandlers,
        ImmutableDictionary<string, IExtensionMessageHandlerWrapper> WorkspaceMessageHandlers);

    private sealed partial class ExtensionMessageHandlerService(
        SolutionServices solutionServices,
        IExtensionMessageHandlerFactory customMessageHandlerFactory)
        : IExtensionMessageHandlerService
    {
        public readonly SolutionServices SolutionServices = solutionServices;
        public readonly IExtensionMessageHandlerFactory CustomMessageHandlerFactory = customMessageHandlerFactory;

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
        private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper>>> _cachedDocumentHandlers_useOnlyUnderLock = new();

        /// <summary>
        /// Cached handlers of non-document-related messages, indexed by handler message name.
        /// </summary>
        private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper>>> _cachedWorkspaceHandlers_useOnlyUnderLock = new();

        private static string GetAssemblyFolderPath(string assemblyFilePath)
        {
            return Path.GetDirectoryName(assemblyFilePath)
                ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");
        }

        private void ClearCachedHandlers_WhileUnderLock()
        {
            Contract.ThrowIfTrue(!Monitor.IsEntered(_gate));
            _cachedWorkspaceHandlers_useOnlyUnderLock.Clear();
            _cachedDocumentHandlers_useOnlyUnderLock.Clear();
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

        private ValueTask<VoidResult> UnregisterExtensionInCurrentProcessAsync(string assemblyFilePath)
        {
            var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

            // Note: unregistering is slightly expensive as we do everything under a lock, to ensure that we have a
            // consistent view of the world.  This is fine as we don't expect this to be called very often.
            ExtensionFolder? folderToUnload = null;
            lock (_gate)
            {
                if (!_folderPathToExtensionFolder_useOnlyUnderLock.TryGetValue(assemblyFolderPath, out var extensionFolder))
                    throw new InvalidOperationException($"No extension registered as '{assemblyFolderPath}'");

                // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within
                // this folder, we can remove the registration for the extension entirely.
                if (extensionFolder.UnregisterAssembly(assemblyFilePath))
                {
                    folderToUnload = extensionFolder;
                    _folderPathToExtensionFolder_useOnlyUnderLock.Remove(assemblyFolderPath);
                }

                // After unregistering, clear out the cached handler names.  They will be recomputed the next time we need them.
                ClearCachedHandlers_WhileUnderLock();
            }

            // Now that we're done with the folder, ask it to unload any resources it is holding onto. This will ask it
            // to unload all ALCs needed to load it and the extensions within.  Unloading will happen once the
            // runtime/gc determine the ALC is finally collectible.
            folderToUnload?.Unload();
            return default;
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

        private ValueTask<VoidResult> ResetInCurrentProcessAsync()
        {
            using var _ = ArrayBuilder<ExtensionFolder>.GetInstance(out var foldersToUnload);

            lock (_gate)
            {
                foldersToUnload.AddRange(_folderPathToExtensionFolder_useOnlyUnderLock.Values);
                _folderPathToExtensionFolder_useOnlyUnderLock.Clear();
                ClearCachedHandlers_WhileUnderLock();
            }

            foreach (var folderToUnload in foldersToUnload)
                folderToUnload.Unload();

            return default;
        }

        private async ValueTask<string> HandleExtensionMessageInCurrentProcessAsync<TArgument>(
            TArgument executeArgument, bool isSolution, string messageName, string jsonMessage,
            Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper>>> cachedHandlers,
            CancellationToken cancellationToken)
        {
            AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper>> lazyHandlers;
            lock (_gate)
            {
                // May be called a lot.  So we use the non-allocating form of this lookup pattern.
                lazyHandlers = cachedHandlers.GetOrAdd(
                    messageName,
                    static (messageName, arg) => AsyncLazy.Create(
                        static (arg, cancellationToken) => arg.@this.ComputeHandlersAsync(arg.messageName, arg.isSolution, cancellationToken),
                        (messageName, arg.@this, arg.isSolution)),
                    (messageName, @this: this, isSolution));
            }

            var handlers = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (handlers.Length == 0)
                throw new InvalidOperationException($"No handler found for message {messageName}.");

            if (handlers.Length > 1)
                throw new InvalidOperationException($"Multiple handlers found for message {messageName}.");

            var handler = (IExtensionMessageHandlerWrapper<TArgument>)handlers[0];

            var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
            var result = await handler.ExecuteAsync(message, executeArgument, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, handler.ResponseType);
        }

        private async Task<ImmutableArray<IExtensionMessageHandlerWrapper>> ComputeHandlersAsync(
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

            using var _ = ArrayBuilder<IExtensionMessageHandlerWrapper>.GetInstance(out var result);
            foreach (var extensionFolder in extensionFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await extensionFolder.AddHandlersAsync(messageName, isSolution, result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutable();
        }
    }
}
