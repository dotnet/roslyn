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
    private readonly SolutionServices _solutionServices = solutionServices;
    private readonly IExtensionMessageHandlerFactory _customMessageHandlerFactory = customMessageHandlerFactory;

    // Core design: To make things lightweight, and to avoid locking, all work is computed and cached in simple
    // immutable dictionaries.  These dictionaries are populated on demand, but contain data that can be recomputed
    // safely if missing.  This allows for a safe approach to 

    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by extension folder path.
    /// </summary>
    private ImmutableDictionary<string, AsyncLazy<ExtensionFolder?>> _folderPathToExtensionFolder = ImmutableDictionary<string, AsyncLazy<ExtensionFolder?>>.Empty;

    /// <summary>
    /// Cached handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>> _cachedDocumentHandlers = ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>>.Empty;

    /// <summary>
    /// Cached handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>> _cachedWorkspaceHandlers = ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>>.Empty;

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
                (service, cancellationToken) => executeOutOfProcessAsync(service, null, cancellationToken),
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
                (service, checksum, cancellationToken) => executeOutOfProcessAsync(service, checksum, cancellationToken),
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
            (service, _, cancellationToken) => service.RegisterExtensionAsync(assemblyFilePath, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<RegisterExtensionResponse> RegisterExtensionInCurrentProcessAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        // var assemblyFileName = Path.GetFileName(assemblyFilePath);
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        // var analyzerAssemblyLoaderProvider = _solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        var lazy = ImmutableInterlocked.GetOrAdd(
            ref _folderPathToExtensionFolder,
            assemblyFolderPath,
            static (assemblyFolderPath, @this) => AsyncLazy.Create(
                cancellationToken => ExtensionFolder.CreateAsync(@this, assemblyFolderPath, cancellationToken)),
            this);

        var extensionFolder = await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
        if (extensionFolder is null)
            throw new InvalidOperationException($"Loading extensions from {assemblyFolderPath} failed.");

        var assemblyHandlers = await extensionFolder.GetAssemblyHandlersAsync(assemblyFilePath, cancellationToken).ConfigureAwait(false);
        if (assemblyHandlers is null)
            throw new InvalidOperationException($"Loading extensions from {assemblyFilePath} failed.");

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
            (service, _, cancellationToken) => service.UnregisterExtensionAsync(assemblyFilePath, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<VoidResult> UnregisterExtensionInCurrentProcessAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        if (_folderPathToExtensionFolder.TryGetValue(assemblyFolderPath, out var extension) &&
            extension != null)
        {
            // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within
            // this folder, we can remove the registration for the extension entirely.
            if (extension.UnregisterHandlers(assemblyFilePath))
                _folderPathToExtensionFolder = _folderPathToExtensionFolder.Remove(assemblyFolderPath);
        }

        // After unregistering, clear out the cached handler names.  They will be recomputed the next time we need them.
        ClearCachedHandlers();

        //Extension? extension = null;
        //using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        //{
        //    if (_folderPathToExtension.TryGetValue(assemblyFolderPath, out extension))
        //    {
        //        // If loading assemblies from this folder failed earlier, don't do anything.
        //        if (extension is null)
        //            return default;

        //        if (extension.RemoveAssemblyHandlers(assemblyFileName, out var assemblyHandlers))
        //        {
        //            if (assemblyHandlers is not null)
        //            {
        //                foreach (var workspaceHandler in assemblyHandlers.WorkspaceMessageHandlers.Keys)
        //                {
        //                    _workspaceHandlers.Remove(workspaceHandler);
        //                }

        //                foreach (var documentHandler in assemblyHandlers.DocumentMessageHandlers.Keys)
        //                {
        //                    _documentHandlers.Remove(documentHandler);
        //                }
        //            }
        //        }

        //        if (extension.AssemblyHandlersCount > 0)
        //            return default;

        //        _folderPathToExtension.Remove(assemblyFolderPath);
        //    }
        //}

        //extension?.AnalyzerAssemblyLoader.Dispose();
        //return default;
    }

    public async ValueTask ResetAsync(CancellationToken cancellationToken)
    {
        await ExecuteInRemoteOrCurrentProcessAsync(
            solution: null,
            cancellationToken => ResetInCurrentProcessAsync(cancellationToken),
            (service, _, cancellationToken) => service.ResetAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private ValueTask<VoidResult> ResetInCurrentProcessAsync(CancellationToken cancellationToken)
    {
        _folderPathToExtensionFolder = ImmutableDictionary<string, AsyncLazy<ExtensionFolder?>>.Empty;
        ClearCachedHandlers();
        return default;
    }

    private void ClearCachedHandlers()
    {
        _cachedWorkspaceHandlers = ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Solution>>>>.Empty;
        _cachedDocumentHandlers = ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>>.Empty;
    }

    public async ValueTask<string> HandleExtensionWorkspaceMessageAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
    {
        return await ExecuteInRemoteOrCurrentProcessAsync(
            solution,
            cancellationToken => HandleExtensionWorkspaceMessageInCurrentProcessAsync(solution, messageName, jsonMessage, cancellationToken),
            (service, checksum, cancellationToken) => service.HandleExtensionWorkspaceMessageAsync(checksum!.Value, messageName, jsonMessage, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<string> HandleExtensionDocumentMessageAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
    {
        return await ExecuteInRemoteOrCurrentProcessAsync(
            document.Project.Solution,
            cancellationToken => HandleExtensionDocumentMessageInCurrentProcessAsync(document, messageName, jsonMessage, cancellationToken),
            (service, checksum, cancellationToken) => service.HandleExtensionDocumentMessageAsync(checksum!.Value, messageName, jsonMessage, document.Id, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<string> HandleExtensionWorkspaceMessageInCurrentProcessAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
    {
        var lazy = _cachedWorkspaceHandlers.GetOrAdd(
            messageName,
            static (messageName, @this) => AsyncLazy.Create(
                static (arg, cancellationToken) => ComputeHandlersAsync(arg.@this, arg.messageName, cancellationToken),
                (messageName, @this)),
            this);

        var handlers = await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
        if (handlers.Length == 0)
            throw new InvalidOperationException($"No handler found for message {messageName}.");

        if (handlers.Length > 1)
            throw new InvalidOperationException($"Multiple handlers found for message {messageName}.");

        var handler = handlers[0];

        try
        {
            var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
            var result = await handler.ExecuteAsync(message, solution, cancellationToken)
                .ConfigureAwait(false);
            var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);
            return responseJson;
        }
        catch
        {
            // Any exception thrown in this method is left to bubble up to the extension.
            // But we unregister all handlers from that assembly to minimize the impact of a bad extension.
            await UnregisterExtensionAsync(assemblyFilePath: handler.ExtensionIdentifier, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<ImmutableArray<IExtensionMessageHandlerWrapper<TResult>>> ComputeHandlersAsync<TResult>(
        ExtensionMessageHandlerService @this, string messageName, bool solution, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>>.GetInstance(out var result);
        foreach (var (_, lazyExtension) in @this._folderPathToExtensionFolder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var extension = await lazyExtension.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (extension is null)
                continue;

            await extension.AddHandlersAsync(messageName, solution, result, cancellationToken).ConfigureAwait(false);
        }

        return result.ToImmutable();
    }

    //private ValueTask<string> HandleExtensionWorkspaceMessageInCurrentProcessAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
    //    => HandleExtensionMessageAsync(solution, messageName, jsonMessage, _workspaceHandlers, cancellationToken);

    //public ValueTask<string> HandleExtensionDocumentMessageInCurrentProcessAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
    //    => HandleExtensionMessageAsync(document, messageName, jsonMessage, _documentHandlers, cancellationToken);

    //private async ValueTask<string> HandleExtensionMessageAsync<TArgument>(
    //    TArgument argument,
    //    string messageName,
    //    string jsonMessage,
    //    ImmutableDictionary<string, IExtensionMessageHandlerWrapper<TArgument>> handlers,
    //    CancellationToken cancellationToken)
    //{

    //    var lazy = _cachedDocumentHandlers.
    //    IExtensionMessageHandlerWrapper<TArgument>? handler;
    //    using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
    //    {
    //        // handlers here is either _workspaceHandlers or _documentHandlers, so it must be protected
    //        // by _lockObject.
    //        if (!handlers.TryGetValue(messageName, out handler))
    //        {
    //            throw new InvalidOperationException($"No handler found for message {messageName}.");
    //        }
    //    }

    //    try
    //    {
    //        var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
    //        var result = await handler.ExecuteAsync(message, argument, cancellationToken)
    //            .ConfigureAwait(false);
    //        var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);
    //        return responseJson;
    //    }
    //    catch
    //    {
    //        // Any exception thrown in this method is left to bubble up to the extension.
    //        // But we unregister all handlers from that assembly to minimize the impact of a bad extension.
    //        await UnregisterExtensionAsync(assemblyFilePath: handler.ExtensionIdentifier, cancellationToken).ConfigureAwait(false);
    //        throw;
    //    }
    //}

    //private async Task RegisterAssemblyAsync(
    //    Extension extension,
    //    string assemblyFileName,
    //    AssemblyHandlers? assemblyHandlers,
    //    CancellationToken cancellationToken)
    //{
    //    using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
    //    {
    //        // Make sure a call to UnloadCustomMessageHandlersAsync hasn't happened while we relinquished the lock on _lockObject
    //        if (!_extensions.TryGetValue(extension.AssemblyFolderPath, out var currentExtension) || !extension.Equals(currentExtension))
    //        {
    //            throw new InvalidOperationException($"Handlers in {extension.AssemblyFolderPath} were unregistered while loading handlers.");
    //        }

    //        try
    //        {
    //            if (assemblyHandlers is not null)
    //            {
    //                var duplicateHandler = _workspaceHandlers.Keys.Intersect(assemblyHandlers.WorkspaceMessageHandlers.Keys).Concat(
    //                _documentHandlers.Keys.Intersect(assemblyHandlers.DocumentMessageHandlers.Keys)).FirstOrDefault();

    //                if (duplicateHandler is not null)
    //                {
    //                    assemblyHandlers = null;
    //                    throw new InvalidOperationException($"Handler name {duplicateHandler} is already registered.");
    //                }

    //                foreach (var handler in assemblyHandlers.WorkspaceMessageHandlers)
    //                {
    //                    _workspaceHandlers.Add(handler.Key, handler.Value);
    //                }

    //                foreach (var handler in assemblyHandlers.DocumentMessageHandlers)
    //                {
    //                    _documentHandlers.Add(handler.Key, handler.Value);
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            extension.SetAssemblyHandlers(assemblyFileName, assemblyHandlers);
    //        }
    //    }
    //}

    private sealed class ExtensionFolder(
        ExtensionMessageHandlerService extensionMessageHandlerService,
        IAnalyzerAssemblyLoaderInternal analyzerAssemblyLoader,
        string assemblyFolderPath)
    {
        private readonly ExtensionMessageHandlerService _extensionMessageHandlerService = extensionMessageHandlerService;
        private readonly IAnalyzerAssemblyLoaderInternal _analyzerAssemblyLoader = analyzerAssemblyLoader;

        // private readonly string _assemblyFolderPath = assemblyFolderPath;

        private ImmutableDictionary<string, AsyncLazy<AssemblyHandlers?>> _assemblyFilePathToHandlers = ImmutableDictionary<string, AsyncLazy<AssemblyHandlers?>>.Empty;

        public static async Task<ExtensionFolder?> CreateAsync(
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

                return new ExtensionFolder(extensionMessageHandlerService, analyzerAssemblyLoader, assemblyFolderPath);
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
                // If loading the assembly fails, we don't want to cache it.
                return null;
            }
        }

        //public void SetAssemblyHandlers(string assemblyFileName, AssemblyHandlers? value)
        //{
        //    EnsureGlobalLockIsOwned();
        //    _assemblies[assemblyFileName] = value;
        //}

        //public bool TryGetAssemblyHandlers(string assemblyFileName, out AssemblyHandlers? value)
        //{
        //    EnsureGlobalLockIsOwned();
        //    return _assemblies.TryGetValue(assemblyFileName, out value);
        //}

        //public bool RemoveAssemblyHandlers(string assemblyFileName, out AssemblyHandlers? value)
        //{
        //    EnsureGlobalLockIsOwned();
        //    return _assemblies.Remove(assemblyFileName, out value);
        //}

        //public int AssemblyHandlersCount
        //{
        //    get
        //    {
        //        EnsureGlobalLockIsOwned();
        //        return _assemblies.Count;
        //    }
        //}

        public async ValueTask<AssemblyHandlers?> GetAssemblyHandlersAsync(
            string assemblyFilePath, CancellationToken cancellationToken)
        {
            var lazy = ImmutableInterlocked.GetOrAdd(
                ref _assemblyFilePathToHandlers,
                assemblyFilePath,
                static (assemblyFilePath, @this) => AsyncLazy.Create(
                    static (args, cancellationToken) => CreateAssemblyHandlersAsync(args.@this, args.assemblyFilePath, cancellationToken),
                    (assemblyFilePath, @this)),
                this);

            return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<AssemblyHandlers?> CreateAssemblyHandlersAsync(
            ExtensionFolder @this, string assemblyFilePath, CancellationToken cancellationToken)
        {
            try
            {
                var assembly = @this._analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                var factory = @this._extensionMessageHandlerService._customMessageHandlerFactory;
                var messageWorkspaceHandlers = factory.CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath)
                    .ToImmutableDictionary(h => h.Name, h => h);
                var messageDocumentHandlers = factory.CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath)
                    .ToImmutableDictionary(h => h.Name, h => h);

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
                // TODO: Log error so it is visible to user.
                return null;
            }
        }

        public async Task AddHandlersAsync<TResult>(string messageName, bool solution, ArrayBuilder<IExtensionMessageHandlerWrapper<TResult>> result, CancellationToken cancellationToken)
        {
            foreach (var (_, lazy) in _assemblyFilePathToHandlers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var handlers = await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (handlers is null)
                    continue;

                if (solution)
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
