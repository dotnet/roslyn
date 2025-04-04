// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
            /// Mapping from assembly file path to the handlers it contains.  Used as its own lock when mutating.
            /// </summary>
            private readonly Dictionary<string, AsyncLazy<AssemblyMessageHandlers>> _assemblyFilePathToHandlers_useOnlyUnderLock = new();

            public ExtensionFolder(
                ExtensionMessageHandlerService extensionMessageHandlerService,
                string assemblyFolderPath)
            {
                _extensionMessageHandlerService = extensionMessageHandlerService;
                _lazyAssemblyLoader = AsyncLazy.Create(cancellationToken =>
                {
#if NET
                    var analyzerAssemblyLoaderProvider = _extensionMessageHandlerService.SolutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
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
                // We only support loading extensions in a .Net Core host.
                return (IAnalyzerAssemblyLoaderInternal?)null;
#endif
                });
            }

            public void Unload()
            {
                // Only if we've created the assembly loader do we need to do anything.
                if (_lazyAssemblyLoader.TryGetValue(out var loader))
                    loader?.Dispose();
            }

            private async Task<AssemblyMessageHandlers> CreateAssemblyHandlersAsync(
                string assemblyFilePath, CancellationToken cancellationToken)
            {
                var analyzerAssemblyLoader = await _lazyAssemblyLoader.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (analyzerAssemblyLoader is null)
                {
                    return new(
                        DocumentMessageHandlers: ImmutableDictionary<string, IExtensionMessageHandlerWrapper>.Empty,
                        WorkspaceMessageHandlers: ImmutableDictionary<string, IExtensionMessageHandlerWrapper>.Empty);
                }

                var assembly = analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                var factory = _extensionMessageHandlerService.CustomMessageHandlerFactory;

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
                lock (_assemblyFilePathToHandlers_useOnlyUnderLock)
                {
                    if (_assemblyFilePathToHandlers_useOnlyUnderLock.ContainsKey(assemblyFilePath))
                        throw new InvalidOperationException($"Extension '{assemblyFilePath}' is already registered.");

                    _assemblyFilePathToHandlers_useOnlyUnderLock.Add(
                        assemblyFilePath,
                        AsyncLazy.Create(
                            cancellationToken => this.CreateAssemblyHandlersAsync(assemblyFilePath, cancellationToken)));
                }
            }

            /// <summary>
            /// Unregisters this assembly path from this extension folder.  If this was the last registered path, then this
            /// will return true so that this folder can be unloaded.
            /// </summary>
            public bool UnregisterAssembly(string assemblyFilePath)
            {
                lock (_assemblyFilePathToHandlers_useOnlyUnderLock)
                {
                    if (!_assemblyFilePathToHandlers_useOnlyUnderLock.ContainsKey(assemblyFilePath))
                        throw new InvalidOperationException($"Extension '{assemblyFilePath}' was not registered.");

                    _assemblyFilePathToHandlers_useOnlyUnderLock.Remove(assemblyFilePath);
                    return _assemblyFilePathToHandlers_useOnlyUnderLock.Count == 0;
                }
            }

            public async ValueTask<AssemblyMessageHandlers> GetAssemblyHandlersAsync(string assemblyFilePath, CancellationToken cancellationToken)
            {
                AsyncLazy<AssemblyMessageHandlers>? lazyHandlers;
                lock (_assemblyFilePathToHandlers_useOnlyUnderLock)
                {
                    if (!_assemblyFilePathToHandlers_useOnlyUnderLock.TryGetValue(assemblyFilePath, out lazyHandlers))
                        throw new InvalidOperationException($"Extension '{assemblyFilePath}' was not registered.");
                }

                // If loading the assembly and getting the handlers failed, then we will throw that exception outwards
                // for the client to hear about.
                return await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }

            public async ValueTask AddHandlersAsync(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper> result, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<AsyncLazy<AssemblyMessageHandlers>>.GetInstance(out var lazyHandlers);

                lock (_assemblyFilePathToHandlers_useOnlyUnderLock)
                {
                    foreach (var (_, lazyHandler) in _assemblyFilePathToHandlers_useOnlyUnderLock)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        lazyHandlers.Add(lazyHandler);
                    }
                }

                foreach (var lazyHandler in lazyHandlers)
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
