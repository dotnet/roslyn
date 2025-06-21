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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

using CachedHandlers = Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper>>>;
using HandlerWrappers = ImmutableArray<IExtensionMessageHandlerWrapper>;

internal sealed partial class ExtensionMessageHandlerServiceFactory
{
    private readonly record struct AssemblyMessageHandlers(
        ImmutableDictionary<string, IExtensionMessageHandlerWrapper> DocumentMessageHandlers,
        ImmutableDictionary<string, IExtensionMessageHandlerWrapper> WorkspaceMessageHandlers,
        Exception? ExtensionException);

    private sealed partial class ExtensionMessageHandlerService(
        SolutionServices solutionServices)
        : IExtensionMessageHandlerService
    {
        private readonly SolutionServices _solutionServices = solutionServices;

        /// <summary>
        /// Lock for <see cref="_folderPathToExtensionFolder"/>, <see cref="_cachedHandlers_useOnlyUnderLock"/>, and
        /// <see cref="_unregisteredHandlerNames_useOnlyUnderLock"/>.  Note: this type is designed such that all time
        /// while this lock is held should be minimal.  Importantly, no async work or IO should be done while holding
        /// this lock.  Instead, all of that work should be pushed into AsyncLazy values that compute when asked,
        /// outside of this lock.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Extensions assembly load contexts and loaded handlers, indexed by extension folder path.
        /// </summary>
        private ImmutableDictionary<string, ExtensionFolder> _folderPathToExtensionFolder = ImmutableDictionary<string, ExtensionFolder>.Empty;

        /// <summary>
        /// Cached handlers of workspace or document related messages, indexed by handler message name.
        /// </summary>
        private readonly (CachedHandlers workspace, CachedHandlers document) _cachedHandlers_useOnlyUnderLock = ([], []);

        /// <summary>
        /// Names of handlers that were previously loaded, but have since been unloaded.  This is used to distinguish a
        /// strict bug, where Gladstone calls into a handler that was never registered, versus a benign case where it is
        /// concurrently calling into a handler that it is also unloading.
        /// </summary>
        private readonly (HashSet<string> workspace, HashSet<string> document) _unregisteredHandlerNames_useOnlyUnderLock = ([], []);

        private static string GetAssemblyFolderPath(string assemblyFilePath)
        {
            return Path.GetDirectoryName(assemblyFilePath)
                ?? throw new InvalidOperationException(string.Format(FeaturesResources.Unable_to_get_the_directory_name_for_0, assemblyFilePath));
        }

        private void ClearCachedHandlers_WhileUnderLock()
        {
            Contract.ThrowIfTrue(!Monitor.IsEntered(_gate));
            _cachedHandlers_useOnlyUnderLock.workspace.Clear();
            _cachedHandlers_useOnlyUnderLock.document.Clear();
        }

        private ValueTask RegisterExtensionInCurrentProcessAsync(string assemblyFilePath)
        {
            // Note: This method executes no extension code.  And, as such, does not try to catch exceptions to
            // translate them accordingly to a failure that we send back to the client as part of the response.

            var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

            // Take lock as we both want to update our state, and the state of the ExtensionFolder instance we get back.
            lock (_gate)
            {
                // Clear out the cached handler names.  They will be recomputed the next time we need them.
                ClearCachedHandlers_WhileUnderLock();

                var extensionFolder = ImmutableInterlocked.GetOrAdd(
                    ref _folderPathToExtensionFolder,
                    assemblyFolderPath,
                    assemblyFolderPath => new ExtensionFolder(this, assemblyFolderPath));

                extensionFolder.RegisterAssembly(assemblyFilePath);
                return default;
            }
        }

        private ValueTask UnregisterExtensionInCurrentProcessAsync(string assemblyFilePath)
        {
            // Note: This method executes no extension code.  And, as such, does not try to catch exceptions to
            // translate them accordingly to a failure that we send back to the client as part of the response.

            var folderToUnload = Unregister();

            // If we're done with the folder, ask it to unload any resources it is holding onto. This will ask it
            // to unload all ALCs needed to load it and the extensions within.  Unloading will happen once the
            // runtime/gc determine the ALC is finally collectible.
            folderToUnload?.Unload();
            return default;

            ExtensionFolder? Unregister()
            {
                var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

                // Take lock as we both want to update our state, and the state of the ExtensionFolder instance we get back.
                lock (_gate)
                {
                    if (!_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var extensionFolder))
                        throw new InvalidOperationException(string.Format(FeaturesResources.No_extension_registered_as_0, assemblyFolderPath));

                    // Clear out the cached handler names.  They will be recomputed the next time we need them.
                    ClearCachedHandlers_WhileUnderLock();

                    var (removeFolder, lazyHandlers) = extensionFolder.UnregisterAssembly(assemblyFilePath);

                    // Add the names of the handlers we loaded for this extension to the unloaded handler set. That way
                    // if we see a call to them in the future, we can report an appropriate message that the extension
                    // is no longer loaded.  Note: if TryGetValue fails, then that means we never called
                    // GetExtensionMessageNamesAsync for this extension.  In which case, we won't know any hanlder names
                    // for it, and we'll have nothing to add to our unregistered handler set.
                    if (lazyHandlers.TryGetValue(out var handlers))
                    {
                        _unregisteredHandlerNames_useOnlyUnderLock.workspace.UnionWith(handlers.WorkspaceMessageHandlers.Keys);
                        _unregisteredHandlerNames_useOnlyUnderLock.document.UnionWith(handlers.DocumentMessageHandlers.Keys);
                    }

                    // If we're not done with the folder.  Return null so our caller doesn't unload anything.
                    // Otherwise, this was the last extension in the folder.  Remove our folder registration entirely,
                    // and return it so the caller can unload it.
                    if (!removeFolder)
                        return null;

                    _folderPathToExtensionFolder = _folderPathToExtensionFolder.Remove(assemblyFolderPath);
                    return extensionFolder;
                }
            }
        }

        private ValueTask ResetInCurrentProcessAsync()
        {
            // Note: This method executes no extension code.  And, as such, does not try to catch exceptions to
            // translate them accordingly to a failure that we send back to the client as part of the response.

            ImmutableDictionary<string, ExtensionFolder> oldFolderPathToExtensionFolder;
            lock (_gate)
            {
                oldFolderPathToExtensionFolder = _folderPathToExtensionFolder;
                _folderPathToExtensionFolder = ImmutableDictionary<string, ExtensionFolder>.Empty;
                _unregisteredHandlerNames_useOnlyUnderLock.workspace.Clear();
                _unregisteredHandlerNames_useOnlyUnderLock.document.Clear();
                ClearCachedHandlers_WhileUnderLock();
            }

            foreach (var (_, folderToUnload) in oldFolderPathToExtensionFolder)
                folderToUnload.Unload();

            return default;
        }

        private async ValueTask<ExtensionMessageNames> GetExtensionMessageNamesInCurrentProcessAsync(
            string assemblyFilePath,
            CancellationToken cancellationToken)
        {
            var assemblyFolderPath = GetAssemblyFolderPath(assemblyFilePath);

            // Throwing here indicates a bug in the gladstone client itself.  So we want this to bubble outwards as a
            // failure that disables extension running in the OOP process.  This must be fixed by gladstone.
            if (!_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var extensionFolder))
                throw new InvalidOperationException(string.Format(FeaturesResources.No_extensions_registered_at_0, assemblyFolderPath));

            // Note if loading the extension assembly failed (due to issues in the extension itself), then the exception
            // produced by it will be passed outwards as data in the ExtensionMessageNames result.
            return await extensionFolder.GetExtensionMessageNamesAsync(assemblyFilePath, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<ExtensionMessageResult> HandleExtensionMessageInCurrentProcessAsync<TArgument>(
            TArgument executeArgument, bool isSolution, string messageName, string jsonMessage,
            CachedHandlers cachedHandlers,
            CancellationToken cancellationToken)
        {
            AsyncLazy<HandlerWrappers> lazyHandlers;
            var potentiallyRefersToUnregisteredHandlerName = false;
            lock (_gate)
            {
                // May be called a lot.  So we use the non-allocating form of this lookup pattern.
                lazyHandlers = cachedHandlers.GetOrAdd(
                    messageName,
                    static (messageName, arg) => AsyncLazy.Create(
                        static (arg, cancellationToken) => arg.@this.ComputeHandlers(arg.messageName, arg.isSolution, cancellationToken),
                        arg),
                    (messageName, @this: this, isSolution));

                var unregisteredHandlerNames = isSolution
                    ? _unregisteredHandlerNames_useOnlyUnderLock.workspace
                    : _unregisteredHandlerNames_useOnlyUnderLock.document;
                potentiallyRefersToUnregisteredHandlerName = unregisteredHandlerNames.Contains(messageName);
            }

            var handlers = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);

            if (handlers.Length == 0)
            {
                // It's ok to find no handlers *if* this was the name of a handler that was previously unloaded. As
                // gladstone allows unloads to happen concurrently with calls to handlers, it's possible that we may
                // unload first, then receive a call to the handler.  In that case, just report back to the client what
                // happened and let them decide what to do.
                if (potentiallyRefersToUnregisteredHandlerName)
                    return new(Response: null, ExtensionWasUnloaded: true, ExtensionException: null);

                // Otherwise, if this was not the name of a handler that was unloaded, then we throw ad this indicates a
                // bug in the gladstone client itself (as it allowed calling into an lsp message that never had
                // registered handlers).  So we want this to bubble outwards as a failure that disables extension
                // running in the OOP process.  This must be fixed by gladstone.
                throw new InvalidOperationException(string.Format(FeaturesResources.No_handler_found_for_message_0, messageName));
            }

            // Throwing here indicates a bug in the gladstone client itself (as it allowed calling into an lsp message
            // that had multiple registered handlers).  So we want this to bubble outwards as a failure that disables
            // extension running in the OOP process.  This must be fixed by gladstone.
            if (handlers.Length > 1)
                throw new InvalidOperationException(string.Format(FeaturesResources.Multiple_handlers_found_for_message_0, messageName));

            var handler = (IExtensionMessageHandlerWrapper<TArgument>)handlers[0];

            // Ensure any non-cancellation exceptions thrown by the extension are caught and returned to the client.  It
            // must not cross the service-hub boundary as an actual exception, as that will tear down our
            // extension-handling OOP service fo all extensions.
            try
            {
                var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
                var result = await handler.ExecuteAsync(message, executeArgument, cancellationToken).ConfigureAwait(false);
                return new(JsonSerializer.Serialize(result, handler.ResponseType), ExtensionWasUnloaded: false, ExtensionException: null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new(Response: "", ExtensionWasUnloaded: false, ExtensionException: ex);
            }
        }

        private HandlerWrappers ComputeHandlers(string messageName, bool isSolution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IExtensionMessageHandlerWrapper>.GetInstance(out var result);

            foreach (var (_, extensionFolder) in _folderPathToExtensionFolder)
                extensionFolder.AddHandlers(messageName, isSolution, result, cancellationToken);

            return result.ToImmutable();
        }
    }
}
