// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions;

[ExportWorkspaceServiceFactory(typeof(IExtensionMessageHandlerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionMessageHandlerServiceFactory(IExtensionMessageHandlerFactory customMessageHandlerFactory)
    : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new ExtensionMessageHandlerService(
            workspaceServices.SolutionServices,
            customMessageHandlerFactory);
}

internal sealed class ExtensionMessageHandlerService(
    SolutionServices solutionServices,
    IExtensionMessageHandlerFactory customMessageHandlerFactory)
    : IExtensionMessageHandlerService
{
    private static readonly ConditionalWeakTable<IExtensionMessageHandlerWrapper, IExtensionMessageHandlerWrapper> s_disabledExtensionHandlers = new();

    private readonly SolutionServices _solutionServices = solutionServices;
    private readonly IExtensionMessageHandlerFactory _customMessageHandlerFactory = customMessageHandlerFactory;

    /// <summary>
    /// Lock for <see cref="_folderPathToExtensionFolder"/>, <see cref="_cachedDocumentHandlers"/>, and <see
    /// cref="_cachedWorkspaceHandlers"/>.  Note: this type is designed such that all time while this lock is held
    /// should be minimal.  Importantly, no async work or IO should be done while holding this lock.  Instead,
    /// all of that work should be pushed into AsyncLazy values that compute when asked, outside of this lock.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by extension folder path.
    /// </summary>
    private readonly Dictionary<string, ExtensionFolder> _folderPathToExtensionFolder = new();

    /// <summary>
    /// Cached handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>> _cachedDocumentHandlers = new();

    /// <summary>
    /// Cached handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>> _cachedWorkspaceHandlers = new();

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
        _cachedWorkspaceHandlers.Clear();
        _cachedDocumentHandlers.Clear();
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
            var extensionFolder = _folderPathToExtensionFolder.GetOrAdd(
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
            if (!_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var extensionFolder))
                throw new InvalidOperationException($"No extension registered as '{assemblyFolderPath}'");

            // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within
            // this folder, we can remove the registration for the extension entirely.
            if (extensionFolder.UnregisterAssembly(assemblyFilePath))
                _folderPathToExtensionFolder.Remove(assemblyFolderPath);

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
            if (!_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out extensionFolder))
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
            _folderPathToExtensionFolder.Clear();
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
                executeArgument: solution, isSolution: true, messageName, jsonMessage, _cachedWorkspaceHandlers, cancellationToken),
            (remoteService, checksum, cancellationToken) => remoteService.HandleExtensionWorkspaceMessageAsync(checksum!.Value, messageName, jsonMessage, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<string> HandleExtensionDocumentMessageAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
    {
        return await ExecuteInRemoteOrCurrentProcessAsync(
            document.Project.Solution,
            cancellationToken => HandleExtensionMessageInCurrentProcessAsync(
                executeArgument: document, isSolution: false, messageName, jsonMessage, _cachedDocumentHandlers, cancellationToken),
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
            foreach (var (_, extensionFolder) in _folderPathToExtensionFolder)
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

    /// <summary>
    /// Represents a folder that many individual extension assemblies can be loaded from.
    /// </summary>
    private sealed class ExtensionFolder
    {
        private readonly ExtensionMessageHandlerService _extensionMessageHandlerService;

        private readonly AsyncLazy<(IAnalyzerAssemblyLoader assemblyLoader, Exception? exception)> _lazyAssemblyLoader;

        /// <summary>
        /// Mapping from assembly file path to the handlers it contains.  Used as its own lock when mutating.
        /// </summary>
        private readonly Dictionary<string, AsyncLazy<(AssemblyHandlers assemblyHandlers, Exception? exception)>> _assemblyFilePathToHandlers = new();

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

        private async Task<(AssemblyHandlers assemblyHandlers, Exception? exception)> CreateAssemblyHandlersAsync(
            string assemblyFilePath, CancellationToken cancellationToken)
        {
            var (analyzerAssemblyLoader, exception) = await _lazyAssemblyLoader.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (exception != null)
                throw exception;

            try
            {
                var assembly = analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                var factory = _extensionMessageHandlerService._customMessageHandlerFactory;

                var messageWorkspaceHandlers = factory
                    .CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                    .ToImmutableDictionary(h => h.Name);
                var messageDocumentHandlers = factory
                    .CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                    .ToImmutableDictionary(h => h.Name);

                return (new AssemblyHandlers()
                {
                    WorkspaceMessageHandlers = messageWorkspaceHandlers,
                    DocumentMessageHandlers = messageDocumentHandlers,
                }, null);
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
            {
                return (AssemblyHandlers.Empty, exception);
            }
        }

        public void RegisterAssembly(string assemblyFilePath)
        {
            lock (_assemblyFilePathToHandlers)
            {
                if (_assemblyFilePathToHandlers.ContainsKey(assemblyFilePath))
                    throw new InvalidOperationException($"Extension '{assemblyFilePath}' is already registered.");

                _assemblyFilePathToHandlers.Add(
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
            lock (_assemblyFilePathToHandlers)
            {
                _assemblyFilePathToHandlers.Remove(assemblyFilePath);
                return _assemblyFilePathToHandlers.Count == 0;
            }
        }

        public async ValueTask<AssemblyHandlers> GetAssemblyHandlersAsync(string assemblyFilePath, CancellationToken cancellationToken)
        {
            AsyncLazy<(AssemblyHandlers assemblyHandlers, Exception? exception)>? lazyHandlers;
            lock (_assemblyFilePathToHandlers)
            {
                if (!_assemblyFilePathToHandlers.TryGetValue(assemblyFilePath, out lazyHandlers))
                    throw new InvalidOperationException($"No extension registered as '{assemblyFilePath}'");
            }

            var (assemblyHandlers, exception) = await lazyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (exception != null)
                throw exception;

            return assemblyHandlers;
        }

        public async ValueTask AddHandlersAsync<TResult>(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>> result, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<AsyncLazy<(AssemblyHandlers assemblyHandlers, Exception? exception)>>.GetInstance(out var lazyHandlers);

            lock (_assemblyFilePathToHandlers)
            {
                foreach (var (_, lazyHandler) in _assemblyFilePathToHandlers)
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

    private sealed class AssemblyHandlers
    {
        public static readonly AssemblyHandlers Empty = new()
        {
            DocumentMessageHandlers = ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Document>>.Empty,
            WorkspaceMessageHandlers = ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Solution>>.Empty,
        };

        /// <summary>
        /// Gets the document-specific handlers that can be passed to <see cref="HandleExtensionDocumentMessageAsync"/>, indexed by their name.
        /// </summary>
        public required ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Document>> DocumentMessageHandlers { get; init; }

        /// <summary>
        /// Gets the non-document-specific handlers that can be passed to <see cref="HandleExtensionWorkspaceMessageAsync"/>, indexed by their name.
        /// </summary>
        public required ImmutableDictionary<string, IExtensionMessageHandlerWrapper<Solution>> WorkspaceMessageHandlers { get; init; }
    }
}
#endif
