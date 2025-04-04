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
        private readonly SolutionServices _solutionServices = solutionServices;
        private readonly IExtensionMessageHandlerFactory _customMessageHandlerFactory = customMessageHandlerFactory;

        /// <summary>
        /// Lock for <see cref="_folderPathToExtensionFolder"/>, <see cref="_cachedDocumentHandlers_useOnlyUnderLock"/>, and <see
        /// cref="_cachedWorkspaceHandlers_useOnlyUnderLock"/>.  Note: this type is designed such that all time while this lock is held
        /// should be minimal.  Importantly, no async work or IO should be done while holding this lock.  Instead,
        /// all of that work should be pushed into AsyncLazy values that compute when asked, outside of this lock.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Extensions assembly load contexts and loaded handlers, indexed by extension folder path.
        /// </summary>
        private ImmutableDictionary<string, ExtensionFolder> _folderPathToExtensionFolder = ImmutableDictionary<string, ExtensionFolder>.Empty;

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

            // Take lock as we both want to update our state, and the state of the ExtensionFolder instance we get back.
            lock (_gate)
            {
                var extensionFolder = ImmutableInterlocked.GetOrAdd(
                    ref _folderPathToExtensionFolder,
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

            // Take lock as we both want to update our state, and the state of the ExtensionFolder instance we get back.
            ExtensionFolder? folderToUnload = null;
            lock (_gate)
            {
                if (!_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var extensionFolder))
                    throw new InvalidOperationException($"No extension registered as '{assemblyFolderPath}'");

                // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within
                // this folder, we can remove the registration for the extension entirely.
                if (extensionFolder.UnregisterAssembly(assemblyFilePath))
                {
                    folderToUnload = extensionFolder;
                    _folderPathToExtensionFolder = _folderPathToExtensionFolder.Remove(assemblyFolderPath);
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

            if (!_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var extensionFolder))
                throw new InvalidOperationException($"No extensions registered at '{assemblyFolderPath}'");

            var assemblyHandlers = await extensionFolder.GetAssemblyHandlersAsync(assemblyFilePath, cancellationToken).ConfigureAwait(false);

            return new(
                [.. assemblyHandlers.WorkspaceMessageHandlers.Keys],
                [.. assemblyHandlers.DocumentMessageHandlers.Keys]);
        }

        private ValueTask<VoidResult> ResetInCurrentProcessAsync()
        {
            ImmutableDictionary<string, ExtensionFolder> oldFolderPathToExtensionFolder;
            lock (_gate)
            {
                oldFolderPathToExtensionFolder = _folderPathToExtensionFolder;
                _folderPathToExtensionFolder = ImmutableDictionary<string, ExtensionFolder>.Empty;
                ClearCachedHandlers_WhileUnderLock();
            }

            foreach (var (_, folderToUnload) in oldFolderPathToExtensionFolder)
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
            using var _ = ArrayBuilder<IExtensionMessageHandlerWrapper>.GetInstance(out var result);

            foreach (var (_, extensionFolder) in _folderPathToExtensionFolder)
                await extensionFolder.AddHandlersAsync(messageName, isSolution, result, cancellationToken).ConfigureAwait(false);

            return result.ToImmutable();
        }
    }
}
