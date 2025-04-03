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
    /// cref="_cachedWorkspaceHandlers"/>.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by extension folder path.
    /// </summary>
    private readonly Dictionary<string, AsyncLazy<IExtensionFolder>> _folderPathToExtensionFolder = new();

    /// <summary>
    /// Cached handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>> _cachedDocumentHandlers = new();

    /// <summary>
    /// Cached handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>> _cachedWorkspaceHandlers = new();

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

    public async ValueTask<RegisterExtensionResponse> RegisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        return await ExecuteInRemoteOrCurrentProcessAsync(
            solution: null,
            cancellationToken => RegisterExtensionInCurrentProcessAsync(assemblyFilePath, cancellationToken),
            (remoteService, _, cancellationToken) => remoteService.RegisterExtensionAsync(assemblyFilePath, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<RegisterExtensionResponse> RegisterExtensionInCurrentProcessAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        // var assemblyFileName = Path.GetFileName(assemblyFilePath);
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        AsyncLazy<IExtensionFolder> lazyExtensionFolder;
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            lazyExtensionFolder = _folderPathToExtensionFolder.GetOrAdd(
                assemblyFolderPath,
                static (assemblyFolderPath, @this) => AsyncLazy.Create(
                    cancellationToken => ExtensionFolder.Create(@this, assemblyFolderPath, cancellationToken)),
                this);

            // After registering, clear out the cached handler names.  They will be recomputed the next time we need them.
            ClearCachedHandlers();
        }

        var extensionFolder = await lazyExtensionFolder.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var lazyAssemblyHandlers = extensionFolder.RegisterAssembly(assemblyFilePath);
        var assemblyHandlers = await lazyAssemblyHandlers.GetValueAsync(cancellationToken).ConfigureAwait(false);

        return new(
            [.. assemblyHandlers.WorkspaceMessageHandlers.Keys],
            [.. assemblyHandlers.DocumentMessageHandlers.Keys]);
    }

    public async ValueTask UnregisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        await ExecuteInRemoteOrCurrentProcessAsync(
            solution: null,
            cancellationToken => UnregisterExtensionInCurrentProcessAsync(assemblyFilePath, cancellationToken),
            (remoteService, _, cancellationToken) => remoteService.UnregisterExtensionAsync(assemblyFilePath, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<VoidResult> UnregisterExtensionInCurrentProcessAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var lazyExtensionFolder))
            {
                var extensionFolder = await lazyExtensionFolder.GetValueAsync(cancellationToken).ConfigureAwait(false);
                // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within
                // this folder, we can remove the registration for the extension entirely.
                if (extensionFolder.UnregisterAssembly(assemblyFilePath))
                    _folderPathToExtensionFolder.Remove(assemblyFolderPath);
            }

            // After unregistering, clear out the cached handler names.  They will be recomputed the next time we need them.
            ClearCachedHandlers();
        }

        return default;
    }

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
            ClearCachedHandlers();
            return default;
        }
    }

    private void ClearCachedHandlers()
    {
        Contract.ThrowIfTrue(!Monitor.IsEntered(_gate));
        _cachedWorkspaceHandlers.Clear();
        _cachedDocumentHandlers.Clear();
    }

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
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            lazyHandlers = cachedHandlers.GetOrAdd(
                messageName,
                static (messageName, arg) => AsyncLazy.Create(
                    static (arg, cancellationToken) => ComputeHandlersAsync<TArgument>(arg.@this, arg.messageName, arg.isSolution, cancellationToken),
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

    private static async Task<ImmutableArray<IExtensionMessageHandlerWrapper<TResult>>> ComputeHandlersAsync<TResult>(
        ExtensionMessageHandlerService @this, string messageName, bool isSolution, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>>.GetInstance(out var result);
        foreach (var (_, lazyExtension) in @this._folderPathToExtensionFolder)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = await lazyExtension.GetValueAsync(cancellationToken).ConfigureAwait(false);
            await extension.AddHandlersAsync(messageName, isSolution, result, cancellationToken).ConfigureAwait(false);
        }

        return result.ToImmutable();
    }

    private interface IExtensionFolder
    {
        AsyncLazy<AssemblyHandlers> RegisterAssembly(string assemblyFilePath);

        /// <summary>
        /// Unregisters this assembly path from this extension folder.  If this was the last registered path, then this
        /// will return true so that this folder can be unloaded.
        /// </summary>
        bool UnregisterAssembly(string assemblyFilePath);

        ValueTask AddHandlersAsync<TResult>(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>> result, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Trivial placeholder impl of <see cref="IExtensionFolder"/> when we fail for some reason to even process the
    /// folder we are told contains extensions.
    /// </summary>
    private sealed class TrivialExtensionFolder : IExtensionFolder
    {
        public static readonly TrivialExtensionFolder Instance = new();

        /// <summary>
        /// No lock needed as registration/unregistration must happen serially.
        /// </summary>
        private readonly List<string> _registeredFilePaths = [];

        public AsyncLazy<AssemblyHandlers> RegisterAssembly(string assemblyFilePath)
        {
            _registeredFilePaths.Add(assemblyFilePath);
            return AsyncLazy.Create(AssemblyHandlers.Empty);
        }

        public bool UnregisterAssembly(string assemblyFilePath)
        {
            _registeredFilePaths.Remove(assemblyFilePath);
            return _registeredFilePaths.Count == 0;
        }

        public ValueTask AddHandlersAsync<TResult>(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>> result, CancellationToken cancellationToken)
            => default;
    }

    private sealed class ExtensionFolder(
        ExtensionMessageHandlerService extensionMessageHandlerService,
        IAnalyzerAssemblyLoaderInternal analyzerAssemblyLoader) : IExtensionFolder
    {
        private readonly ExtensionMessageHandlerService _extensionMessageHandlerService = extensionMessageHandlerService;
        private readonly IAnalyzerAssemblyLoaderInternal _analyzerAssemblyLoader = analyzerAssemblyLoader;

        private ImmutableDictionary<string, AsyncLazy<AssemblyHandlers>> _assemblyFilePathToHandlers = ImmutableDictionary<string, AsyncLazy<AssemblyHandlers>>.Empty;

        public static IExtensionFolder Create(
            ExtensionMessageHandlerService extensionMessageHandlerService,
            string assemblyFolderPath,
            CancellationToken cancellationToken)
        {
            var analyzerAssemblyLoaderProvider = extensionMessageHandlerService._solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
            var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();

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

                return new ExtensionFolder(extensionMessageHandlerService, analyzerAssemblyLoader);
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
                // TODO: Log this exception so the client knows something went wrong.
                return new TrivialExtensionFolder();
            }
        }

        public AsyncLazy<AssemblyHandlers> RegisterAssembly(string assemblyFilePath)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _assemblyFilePathToHandlers,
                assemblyFilePath,
                static (assemblyFilePath, @this) => AsyncLazy.Create(
                    static (args, cancellationToken) => CreateAssemblyHandlers(args.@this, args.assemblyFilePath, cancellationToken),
                    (assemblyFilePath, @this)),
                this);
        }

        private static AssemblyHandlers CreateAssemblyHandlers(
            ExtensionFolder @this, string assemblyFilePath, CancellationToken cancellationToken)
        {
            try
            {
                var assembly = @this._analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                var factory = @this._extensionMessageHandlerService._customMessageHandlerFactory;

                var messageWorkspaceHandlers = factory
                    .CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                    .ToImmutableDictionary(h => h.Name);
                var messageDocumentHandlers = factory
                    .CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath, cancellationToken)
                    .ToImmutableDictionary(h => h.Name);

                return new AssemblyHandlers()
                {
                    WorkspaceMessageHandlers = messageWorkspaceHandlers,
                    DocumentMessageHandlers = messageDocumentHandlers,
                };

                // We don't add assemblyHandlers to _assemblies here and instead let _extensionMessageHandlerService.RegisterAssembly do it
                // since RegisterAssembly can still fail if there are duplicated handler names.
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
            {
                // TODO: Log this exception so the client knows something went wrong.
                return AssemblyHandlers.Empty;
            }
        }

        public async ValueTask AddHandlersAsync<TResult>(string messageName, bool isSolution, ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>> result, CancellationToken cancellationToken)
        {
            foreach (var (_, lazy) in _assemblyFilePathToHandlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var handlers = await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
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

        public bool UnregisterAssembly(string assemblyFilePath)
        {
            _assemblyFilePathToHandlers.Remove(assemblyFilePath);
            return _assemblyFilePathToHandlers.IsEmpty;
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
