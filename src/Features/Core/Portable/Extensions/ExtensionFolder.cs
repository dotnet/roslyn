// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

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
    private sealed partial class ExtensionMessageHandlerService
    {
        /// <summary>
        /// Represents a folder that many individual extension assemblies can be loaded from.
        /// </summary>
        private sealed class ExtensionFolder
        {
            private readonly ExtensionMessageHandlerService _extensionMessageHandlerService;

            /// <summary>
            /// Lazily computed assembly loader for this particular folder, as well as any exception that occurred while
            /// we were trying to enumerate files in it, or add assemblies in it as dependency locations.
            /// </summary>
            private readonly AsyncLazy<(IAnalyzerAssemblyLoader assemblyLoader, Exception? exception)> _lazyAssemblyLoader;

            /// <summary>
            /// Mapping from assembly file path to the handlers it contains.  Used as its own lock when mutating.
            /// </summary>
            private readonly Dictionary<string, AsyncLazy<(AssemblyMessageHandlers assemblyHandlers, Exception? exception)>> _assemblyFilePathToHandlers_useOnlyUnderLock = new();

            public ExtensionFolder(
                ExtensionMessageHandlerService extensionMessageHandlerService,
                string assemblyFolderPath)
            {
                _extensionMessageHandlerService = extensionMessageHandlerService;
                _lazyAssemblyLoader = AsyncLazy.Create(cancellationToken =>
                {
                    var analyzerAssemblyLoaderProvider = _extensionMessageHandlerService._solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
                    var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();
                    Exception? exception = null;
                    try
                    {
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
                    }
                    catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
                    {
                        exception = ex;
                    }

                    return ((IAnalyzerAssemblyLoader)analyzerAssemblyLoader, exception);
                });
            }

            private async Task<(AssemblyMessageHandlers assemblyHandlers, Exception? exception)> CreateAssemblyHandlersAsync(
                string assemblyFilePath, CancellationToken cancellationToken)
            {
                // If creating the underlying assembly loader failed, then we will throw that exception outwards for the
                // client to hear about.
                var (analyzerAssemblyLoader, exception) = await _lazyAssemblyLoader.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (exception != null)
                    throw exception;

                ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Document>> documentMessageHandlers;
                ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Solution>> workspaceMessageHandlers;

                try
                {
                    var assembly = analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                    var factory = _extensionMessageHandlerService._customMessageHandlerFactory;

                    documentMessageHandlers = factory
                        .CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                        .ToImmutableDictionary(h => h.Name);
                    workspaceMessageHandlers = factory
                        .CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                        .ToImmutableDictionary(h => h.Name);
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
                {
                    documentMessageHandlers = ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Document>>.Empty;
                    workspaceMessageHandlers = ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Solution>>.Empty;
                    exception = ex;
                }

                return (new(documentMessageHandlers, workspaceMessageHandlers), exception);
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
                    _assemblyFilePathToHandlers_useOnlyUnderLock.Remove(assemblyFilePath);
                    return _assemblyFilePathToHandlers_useOnlyUnderLock.Count == 0;
                }
            }

            public async ValueTask<AssemblyMessageHandlers> GetAssemblyHandlersAsync(string assemblyFilePath, CancellationToken cancellationToken)
            {
                AsyncLazy<(AssemblyMessageHandlers assemblyHandlers, Exception? exception)>? lazyHandlers;
                lock (_assemblyFilePathToHandlers_useOnlyUnderLock)
                {
                    if (!_assemblyFilePathToHandlers_useOnlyUnderLock.TryGetValue(assemblyFilePath, out lazyHandlers))
                        throw new InvalidOperationException($"No extension registered as '{assemblyFilePath}'");
                }

                // If loading the assembly and getting the handlers failed, then we will throw that exception outwards
                // for the client to hear about.
                var (assemblyHandlers, exception) = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (exception != null)
                    throw exception;

                return assemblyHandlers;
            }

            public async ValueTask AddHandlersAsync<TResult>(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>> result, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<AsyncLazy<(AssemblyMessageHandlers assemblyHandlers, Exception? exception)>>.GetInstance(out var lazyHandlers);

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

                    var (handlers, _) = await lazyHandler.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    if (isSolution)
                    {
                        if (handlers.WorkspaceMessageHandlers.TryGetValue(messageName, out var handler))
                            result.Add((IExtensionMessageHandlerWrapper<TResult>)handler);
                    }
                    else
                    {
                        if (handlers.DocumentMessageHandlers.TryGetValue(messageName, out var handler))
                            result.Add((IExtensionMessageHandlerWrapper<TResult>)handler);
                    }
                }
            }
        }
    }
}
#endif
