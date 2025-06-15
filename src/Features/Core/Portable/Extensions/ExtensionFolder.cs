// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

using HandlerMap = ImmutableDictionary<string, IExtensionMessageHandlerWrapper>;

internal sealed partial class ExtensionMessageHandlerServiceFactory
{
    private partial class ExtensionMessageHandlerService
    {
        /// <summary>
        /// Represents a folder that many individual extension assemblies can be loaded from.
        /// </summary>
        private sealed class ExtensionFolder
        {
            private readonly ExtensionMessageHandlerService _extensionMessageHandlerService;

            /// <summary>
            /// Lazily computed assembly loader for this particular folder.
            /// </summary>
            private readonly AsyncLazy<(IExtensionAssemblyLoader? assemblyLoader, Exception? extensionException)> _lazyAssemblyLoader;

            /// <summary>
            /// Mapping from assembly file path to the handlers it contains.  Should only be mutated while the <see
            /// cref="_gate"/> lock is held by our parent <see cref="_extensionMessageHandlerService"/>.
            /// </summary>
            private ImmutableDictionary<string, AsyncLazy<AssemblyMessageHandlers>> _assemblyFilePathToHandlers = ImmutableDictionary<string, AsyncLazy<AssemblyMessageHandlers>>.Empty;

            public ExtensionFolder(
                ExtensionMessageHandlerService extensionMessageHandlerService,
                string assemblyFolderPath)
            {
                _extensionMessageHandlerService = extensionMessageHandlerService;
                _lazyAssemblyLoader = AsyncLazy.Create(cancellationToken =>
                {
                    var analyzerAssemblyLoaderProvider = _extensionMessageHandlerService._solutionServices.GetRequiredService<IExtensionAssemblyLoaderProvider>();
                    return analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader(assemblyFolderPath, cancellationToken);
                });
            }

            public void Unload()
            {
                // Only if we've created the assembly loader do we need to do anything.
                _lazyAssemblyLoader.TryGetValue(out var tuple);
                tuple.assemblyLoader?.Unload();
            }

            private async Task<AssemblyMessageHandlers> CreateAssemblyHandlersAsync(
                string assemblyFilePath, CancellationToken cancellationToken)
            {
                // On NetFramework analyzerAssemblyLoader. As we have no way to load extensions safely, just return an
                // empty set of handlers.  Similarly, if we ran into an exception enumerating the exception folder, then
                // pass that upwards as well.  This exception will be reported back to the client.  By passing back
                // empty handler arrays, our higher layers can operate properly and treat this as an assembly with
                // nothing to offer.
                var (analyzerAssemblyLoader, extensionException) = await _lazyAssemblyLoader.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (analyzerAssemblyLoader is null || extensionException is not null)
                {
                    return new(
                        DocumentMessageHandlers: HandlerMap.Empty,
                        WorkspaceMessageHandlers: HandlerMap.Empty,
                        extensionException);
                }

                var assembly = analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                var factory = _extensionMessageHandlerService._solutionServices.GetService<IExtensionMessageHandlerFactory>();
                if (factory is null)
                {
                    return new(
                        DocumentMessageHandlers: HandlerMap.Empty,
                        WorkspaceMessageHandlers: HandlerMap.Empty,
                        ExtensionException: null);
                }

                // We're calling into code here to analyze the assembly at the specified file and to create handlers we
                // find within it.  If this throws, then we will capture that exception and return it to the caller to 
                // let it decide what to do.
                try
                {
                    var documentMessageHandlers = factory
                        .CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                        .ToImmutableDictionary(h => h.Name, h => (IExtensionMessageHandlerWrapper)h);
                    var workspaceMessageHandlers = factory
                        .CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                        .ToImmutableDictionary(h => h.Name, h => (IExtensionMessageHandlerWrapper)h);

                    return new(
                        DocumentMessageHandlers: documentMessageHandlers,
                        WorkspaceMessageHandlers: workspaceMessageHandlers,
                        ExtensionException: null);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // In the case of an exception, act as if the extension has no handlers to proffer.  Also capture
                    // the exception so it can be reported back to the client.
                    return new(
                        DocumentMessageHandlers: HandlerMap.Empty,
                        WorkspaceMessageHandlers: HandlerMap.Empty,
                        ex);
                }
            }

            public void RegisterAssembly(string assemblyFilePath)
            {
                // Must be called under our parent's lock to ensure we see a consistent state of things.
                // This allows us to safely examine our current state, and then add the new item.
                Contract.ThrowIfTrue(!Monitor.IsEntered(_extensionMessageHandlerService._gate));

                // If this throws, it also indicated a bug in gladstone that must be fixed.  As such, it is ok if this
                // tears down the extension service in OOP.
                if (_assemblyFilePathToHandlers.ContainsKey(assemblyFilePath))
                    throw new InvalidOperationException(string.Format(FeaturesResources.Extension_0_is_already_registered, assemblyFilePath));

                _assemblyFilePathToHandlers = _assemblyFilePathToHandlers.Add(
                   assemblyFilePath,
                   AsyncLazy.Create(
                       cancellationToken => this.CreateAssemblyHandlersAsync(assemblyFilePath, cancellationToken)));
            }

            /// <summary>
            /// Unregisters this assembly path from this extension folder.  If this was the last registered path, then
            /// this will return true so that this folder can be unloaded.  Also returns the lazy handlers for this
            /// assembly path.  If <see cref="GetExtensionMessageNamesAsync"/> has been called, this will be a fully
            /// computed value. Otherwise, it will be an uncomputed value.
            /// </summary>
            public (bool removeFolder, AsyncLazy<AssemblyMessageHandlers> lazyHandlers) UnregisterAssembly(string assemblyFilePath)
            {
                // Must be called under our parent's lock to ensure we see a consistent state of things. This allows us
                // to safely examine our current state, remove the existing item, and then return if we are now empty.
                Contract.ThrowIfTrue(!Monitor.IsEntered(_extensionMessageHandlerService._gate));

                // If this throws, it also indicated a bug in gladstone that must be fixed.  As such, it is ok if this
                // tears down the extension service in OOP.
                if (!_assemblyFilePathToHandlers.TryGetValue(assemblyFilePath, out var lazyHandlers))
                    throw new InvalidOperationException(string.Format(FeaturesResources.Extension_0_was_not_registered, assemblyFilePath));

                _assemblyFilePathToHandlers = _assemblyFilePathToHandlers.Remove(assemblyFilePath);
                return (_assemblyFilePathToHandlers.Count == 0, lazyHandlers);
            }

            public async ValueTask<ExtensionMessageNames> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken)
            {
                // This is safe to do as our general contract is that all handler operations should be called explicitly
                // between calls to Register/Unregister the extension.  So this cannot race with an extension being
                // removed.
                //
                // If this throws, it also indicated a bug in gladstone that must be fixed.  As such, it is ok if this
                // tears down the extension service in OOP.
                if (!_assemblyFilePathToHandlers.TryGetValue(assemblyFilePath, out var lazyHandlers))
                    throw new InvalidOperationException(string.Format(FeaturesResources.Extension_0_was_not_registered, assemblyFilePath));

                // Handlers already encapsulates any extension-level exceptions that occurred when loading the assembly.
                // As such, we don't need our own try/catch here.  We can just return the result directly.
                var handlers = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return new(
                    WorkspaceMessageHandlers: [.. handlers.WorkspaceMessageHandlers.Keys],
                    DocumentMessageHandlers: [.. handlers.DocumentMessageHandlers.Keys],
                    ExtensionException: handlers.ExtensionException);
            }

            public void AddHandlers(
                string messageName,
                bool isSolution,
                ArrayBuilder<IExtensionMessageHandlerWrapper> result,
                CancellationToken cancellationToken)
            {
                foreach (var (_, lazyHandler) in _assemblyFilePathToHandlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Note1: We will only be adding handlers for for the specific messageName we're being asked for.
                    // However that message name will only be known for extensions we've actually loaded handlers for.
                    // So we can just synchronously only process lazyHandlers that have values already computed for
                    // them.  We don't need to compute them here.

                    // Note1 that if loading the handlers from the assembly failed, then getting this value will still
                    // succeed. It will just give us back an empty set of handlers, which will effectively be a no-op.
                    if (lazyHandler.TryGetValue(out var handlers))
                    {
                        var specificHandlers = isSolution ? handlers.WorkspaceMessageHandlers : handlers.DocumentMessageHandlers;
                        if (specificHandlers.TryGetValue(messageName, out var handler))
                            result.Add(handler);
                    }
                }
            }
        }
    }
}
