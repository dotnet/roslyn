// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

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
            private readonly AsyncLazy<IAnalyzerAssemblyLoaderInternal?> _lazyAssemblyLoader;

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
#if NET
                    var analyzerAssemblyLoaderProvider = _extensionMessageHandlerService._solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
                    var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();

                    // Allow this assembly loader to load any dll in assemblyFolderPath.
                    foreach (var dll in Directory.EnumerateFiles(assemblyFolderPath, "*.dll"))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            // Check if the file is a valid .NET assembly.
                            AssemblyName.GetAssemblyName(dll);
                        }
                        catch
                        {
                            // The file is not a valid .NET assembly, skip it.
                            continue;
                        }

                        analyzerAssemblyLoader.AddDependencyLocation(dll);
                    }

                    return (IAnalyzerAssemblyLoaderInternal?)analyzerAssemblyLoader;
#else
                    return (IAnalyzerAssemblyLoaderInternal?)null;
#endif
                });
            }

            public void Unload()
            {
                // Only if we've created the assembly loader do we need to do anything.
                _lazyAssemblyLoader.TryGetValue(out var loader);
                loader?.Dispose();
            }

            private async Task<AssemblyMessageHandlers> CreateAssemblyHandlersAsync(
                string assemblyFilePath, CancellationToken cancellationToken)
            {
                // On NetFramework do nothing. We have no way to load extensions safely.
                var analyzerAssemblyLoader = await _lazyAssemblyLoader.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (analyzerAssemblyLoader is null)
                {
                    return new(
                        DocumentMessageHandlers: ImmutableDictionary<string, IExtensionMessageHandlerWrapper>.Empty,
                        WorkspaceMessageHandlers: ImmutableDictionary<string, IExtensionMessageHandlerWrapper>.Empty);
                }

                var assembly = analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                var factory = _extensionMessageHandlerService._customMessageHandlerFactory;
                Contract.ThrowIfNull(factory);

                var documentMessageHandlers = factory
                    .CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                    .ToImmutableDictionary(h => h.Name, h => (IExtensionMessageHandlerWrapper)h);
                var workspaceMessageHandlers = factory
                    .CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                    .ToImmutableDictionary(h => h.Name, h => (IExtensionMessageHandlerWrapper)h);

                return new(documentMessageHandlers, workspaceMessageHandlers);
            }

            public void RegisterAssembly(string assemblyFilePath)
            {
                // Must be called under our parent's lock to ensure we see a consistent state of things.
                // This allows us to safely examine our current state, and then add the new item.
                Contract.ThrowIfTrue(!Monitor.IsEntered(_extensionMessageHandlerService._gate));

                if (_assemblyFilePathToHandlers.ContainsKey(assemblyFilePath))
                    throw new InvalidOperationException($"Extension '{assemblyFilePath}' is already registered.");

                _assemblyFilePathToHandlers = _assemblyFilePathToHandlers.Add(
                   assemblyFilePath,
                   AsyncLazy.Create(
                       cancellationToken => this.CreateAssemblyHandlersAsync(assemblyFilePath, cancellationToken)));
            }

            /// <summary>
            /// Unregisters this assembly path from this extension folder.  If this was the last registered path, then this
            /// will return true so that this folder can be unloaded.
            /// </summary>
            public bool UnregisterAssembly(string assemblyFilePath)
            {
                // Must be called under our parent's lock to ensure we see a consistent state of things. This allows us
                // to safely examine our current state, remove the existing item, and then return if we are now empty.
                Contract.ThrowIfTrue(!Monitor.IsEntered(_extensionMessageHandlerService._gate));

                if (!_assemblyFilePathToHandlers.ContainsKey(assemblyFilePath))
                    throw new InvalidOperationException($"Extension '{assemblyFilePath}' was not registered.");

                _assemblyFilePathToHandlers = _assemblyFilePathToHandlers.Remove(assemblyFilePath);
                return _assemblyFilePathToHandlers.Count == 0;
            }

            public async ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken)
            {
                // This is safe to do as our general contract is that all handler operations should be called explicitly
                // between calls to Register/Unregister the extension.  So this cannot race with an extension being
                // removed.
                if (!_assemblyFilePathToHandlers.TryGetValue(assemblyFilePath, out var lazyHandlers))
                    throw new InvalidOperationException($"Extension '{assemblyFilePath}' was not registered.");

                // If loading the assembly and getting the handlers failed, then we will throw that exception outwards
                // for the client to hear about.
                var handlers = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);

                return new(
                    WorkspaceMessageHandlers: [.. handlers.WorkspaceMessageHandlers.Keys],
                    DocumentMessageHandlers: [.. handlers.DocumentMessageHandlers.Keys]);
            }

            public async ValueTask AddHandlersAsync(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper> result, CancellationToken cancellationToken)
            {
                foreach (var (_, lazyHandler) in _assemblyFilePathToHandlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    AssemblyMessageHandlers handlers;
                    try
                    {
                        handlers = await lazyHandler.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
                    {
                        // If loading the assembly and getting the handlers failed, then we will ignore this assembly
                        // and continue on to the next one.
                        continue;
                    }

                    var specificHandlers = isSolution ? handlers.WorkspaceMessageHandlers : handlers.DocumentMessageHandlers;
                    if (specificHandlers.TryGetValue(messageName, out var handler))
                        result.Add(handler);
                }
            }
        }
    }
}
