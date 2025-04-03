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

    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by handler file path. The handlers are indexed by type name.
    /// </summary>
    private ImmutableDictionary<string, AsyncLazy<Extension?>> _extensions = ImmutableDictionary<string, AsyncLazy<Extension?>>.Empty;

    /// <summary>
    /// Handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>> _cachedDocumentHandlers = ImmutableDictionary<string, AsyncLazy<ImmutableArray<IExtensionMessageHandlerWrapper<Document>>>>.Empty;

    /// <summary>
    /// Handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private ImmutableDictionary<string, AsyncLazy<IExtensionMessageHandlerWrapper<Solution>?>> _cachedWorkspaceHandlers = ImmutableDictionary<string, AsyncLazy<IExtensionMessageHandlerWrapper<Solution>?>>.Empty;

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
        var assemblyFileName = Path.GetFileName(assemblyFilePath);
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        // var analyzerAssemblyLoaderProvider = _solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        var lazy = ImmutableInterlocked.GetOrAdd(
            ref _extensions,
            assemblyFolderPath,
            static (assemblyFolderPath, @this) => AsyncLazy.Create(
                cancellationToken => Extension.CreateAsync(@this, assemblyFolderPath, cancellationToken)),
            this);

        var extension = await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
        if (extension is null)
            throw new InvalidOperationException($"A loading assemblies from {assemblyFolderPath} failed.");

        //Extension? extension;
        //using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        //{
        //    // Check if the assembly is already loaded.
        //    if (!_extensions.TryGetValue(assemblyFolderPath, out extension))
        //    {
        //        try
        //        {
        //            var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();

        //            // Allow this assembly loader to load any dll in assemblyFolderPath.
        //            foreach (var dll in Directory.EnumerateFiles(assemblyFolderPath, "*.dll"))
        //            {
        //                try
        //                {
        //                    // Check if the file is a valid .NET assembly.
        //                    AssemblyName.GetAssemblyName(dll);
        //                }
        //                catch
        //                {
        //                    // The file is not a valid .NET assembly, skip it.
        //                    continue;
        //                }

        //                analyzerAssemblyLoader.AddDependencyLocation(dll);
        //            }

        //            extension = new Extension(this, analyzerAssemblyLoader, assemblyFolderPath);
        //        }
        //        catch
        //        {
        //            _extensions[assemblyFolderPath] = null;
        //            throw;
        //        }

        //        _extensions[assemblyFolderPath] = extension;
        //    }

        //    if (extension is null)
        //    {
        //        throw new InvalidOperationException($"A previous attempt to load assemblies from {assemblyFolderPath} failed.");
        //    }

        //    if (extension.TryGetAssemblyHandlers(assemblyFileName, out var assemblyHandlers))
        //    {
        //        if (assemblyHandlers is null)
        //        {
        //            throw new InvalidOperationException($"A previous attempt to load {assemblyFilePath} failed.");
        //        }

        //        return new(
        //            [.. assemblyHandlers.WorkspaceMessageHandlers.Keys],
        //            [.. assemblyHandlers.DocumentMessageHandlers.Keys]);
        //    }
        //}

        //// Intentionally call this outside of the lock.
        //return await extension.LoadAssemblyAsync(assemblyFileName, cancellationToken).ConfigureAwait(false);
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
        var assemblyFileName = Path.GetFileName(assemblyFilePath);
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        if (_extensions.TryGetValue(assemblyFolderPath, out var extension) &&
            extension != null)
        {
            // Unregister this particular assembly file from teh assembly folder.  If it was the last extension within this folder,
            // we can remove the registeration for the extension entirely.
            if (extension.Unregister(assemblyFileName))
                _extensions = _extensions.Remove(assemblyFolderPath);
        }

        // After unregistering, clear out the cached handler names.  They will be recomputed the next time we need them.
        ClearCachedHandlers();

        Extension? extension = null;
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_extensions.TryGetValue(assemblyFolderPath, out extension))
            {
                // If loading assemblies from this folder failed earlier, don't do anything.
                if (extension is null)
                    return default;

                if (extension.RemoveAssemblyHandlers(assemblyFileName, out var assemblyHandlers))
                {
                    if (assemblyHandlers is not null)
                    {
                        foreach (var workspaceHandler in assemblyHandlers.WorkspaceMessageHandlers.Keys)
                        {
                            _workspaceHandlers.Remove(workspaceHandler);
                        }

                        foreach (var documentHandler in assemblyHandlers.DocumentMessageHandlers.Keys)
                        {
                            _documentHandlers.Remove(documentHandler);
                        }
                    }
                }

                if (extension.AssemblyHandlersCount > 0)
                    return default;

                _extensions.Remove(assemblyFolderPath);
            }
        }

        extension?.AnalyzerAssemblyLoader.Dispose();
        return default;
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
        _extensions = ImmutableDictionary<string, AsyncLazy<Extension?>>.Empty;
        return default;
    }

    private void ClearCachedHandlers()
    {
        _workspaceHandlers = ImmutableDictionary<string, AsyncLazy<IExtensionMessageHandlerWrapper<Solution>?>>.Empty;
        _documentHandlers = ImmutableDictionary<string, AsyncLazy<IExtensionMessageHandlerWrapper<Document>?>>.Empty;
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

    private ValueTask<string> HandleExtensionWorkspaceMessageInCurrentProcess1Async(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
    {
        var lazy = _cachedWorkspaceHandlers.GetOrAdd(
            messageName,
            (messageName, arg) => AsyncLazy.Create(cancellationToken => ComputeHandler(arg.@this),
            (@this: this, solution, jsonMessage))
    }

    private ValueTask<string> HandleExtensionWorkspaceMessageInCurrentProcessAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
        => HandleExtensionMessageAsync(solution, messageName, jsonMessage, _workspaceHandlers, cancellationToken);

    public ValueTask<string> HandleExtensionDocumentMessageInCurrentProcessAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
        => HandleExtensionMessageAsync(document, messageName, jsonMessage, _documentHandlers, cancellationToken);

    private async ValueTask<string> HandleExtensionMessageAsync<TArgument>(
        TArgument argument,
        string messageName,
        string jsonMessage,
        ImmutableDictionary<string, IExtensionMessageHandlerWrapper<TArgument>> handlers,
        CancellationToken cancellationToken)
    {

        var lazy = _cachedDocumentHandlers.
        IExtensionMessageHandlerWrapper<TArgument>? handler;
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            // handlers here is either _workspaceHandlers or _documentHandlers, so it must be protected
            // by _lockObject.
            if (!handlers.TryGetValue(messageName, out handler))
            {
                throw new InvalidOperationException($"No handler found for message {messageName}.");
            }
        }

        try
        {
            var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
            var result = await handler.ExecuteAsync(message, argument, cancellationToken)
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

    private async Task RegisterAssemblyAsync(
        Extension extension,
        string assemblyFileName,
        AssemblyHandlers? assemblyHandlers,
        CancellationToken cancellationToken)
    {
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            // Make sure a call to UnloadCustomMessageHandlersAsync hasn't happened while we relinquished the lock on _lockObject
            if (!_extensions.TryGetValue(extension.AssemblyFolderPath, out var currentExtension) || !extension.Equals(currentExtension))
            {
                throw new InvalidOperationException($"Handlers in {extension.AssemblyFolderPath} were unregistered while loading handlers.");
            }

            try
            {
                if (assemblyHandlers is not null)
                {
                    var duplicateHandler = _workspaceHandlers.Keys.Intersect(assemblyHandlers.WorkspaceMessageHandlers.Keys).Concat(
                    _documentHandlers.Keys.Intersect(assemblyHandlers.DocumentMessageHandlers.Keys)).FirstOrDefault();

                    if (duplicateHandler is not null)
                    {
                        assemblyHandlers = null;
                        throw new InvalidOperationException($"Handler name {duplicateHandler} is already registered.");
                    }

                    foreach (var handler in assemblyHandlers.WorkspaceMessageHandlers)
                    {
                        _workspaceHandlers.Add(handler.Key, handler.Value);
                    }

                    foreach (var handler in assemblyHandlers.DocumentMessageHandlers)
                    {
                        _documentHandlers.Add(handler.Key, handler.Value);
                    }
                }
            }
            finally
            {
                extension.SetAssemblyHandlers(assemblyFileName, assemblyHandlers);
            }
        }
    }

    private sealed class Extension(
        ExtensionMessageHandlerService extensionMessageHandlerService,
        IAnalyzerAssemblyLoaderInternal analyzerAssemblyLoader,
        string assemblyFolderPath)
    {
        private readonly ExtensionMessageHandlerService _extensionMessageHandlerService = extensionMessageHandlerService;
        private readonly IAnalyzerAssemblyLoaderInternal _analyzerAssemblyLoader = analyzerAssemblyLoader;

        private readonly string _assemblyFolderPath = assemblyFolderPath;

        private readonly Dictionary<string, AssemblyHandlers?> _assemblies = new();

        public static async Task<Extension?> CreateAsync(
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

                return new Extension(extensionMessageHandlerService, analyzerAssemblyLoader, assemblyFolderPath);
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
                // If loading the assembly fails, we don't want to cache it.
                return null;
            }
        }

        public void SetAssemblyHandlers(string assemblyFileName, AssemblyHandlers? value)
        {
            EnsureGlobalLockIsOwned();
            _assemblies[assemblyFileName] = value;
        }

        public bool TryGetAssemblyHandlers(string assemblyFileName, out AssemblyHandlers? value)
        {
            EnsureGlobalLockIsOwned();
            return _assemblies.TryGetValue(assemblyFileName, out value);
        }

        public bool RemoveAssemblyHandlers(string assemblyFileName, out AssemblyHandlers? value)
        {
            EnsureGlobalLockIsOwned();
            return _assemblies.Remove(assemblyFileName, out value);
        }

        public int AssemblyHandlersCount
        {
            get
            {
                EnsureGlobalLockIsOwned();
                return _assemblies.Count;
            }
        }

        public async ValueTask<RegisterExtensionResponse> LoadAssemblyAsync(
            string assemblyFileName, CancellationToken cancellationToken)
        {
            var assemblyFilePath = Path.Combine(AssemblyFolderPath, assemblyFileName);

            // AssemblyLoadLockObject is only used to avoid multiple calls from the same extensions to load the same assembly concurrently
            // resulting in the constructors of the same handlers being called more than once.
            // All other concurrent operations, including modifying extension.Assemblies are protected by _lockObject.
            using (await _assemblyLoadLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                AssemblyHandlers? assemblyHandlers = null;
                Exception? exception = null;
                try
                {
                    var assembly = _analyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                    var factory = _extensionMessageHandlerService._customMessageHandlerFactory;
                    var messageWorkspaceHandlers = factory.CreateWorkspaceMessageHandlers(assembly, extensionIdentifier: assemblyFilePath)
                        .ToImmutableDictionary(h => h.Name, h => h);
                    var messageDocumentHandlers = factory.CreateDocumentMessageHandlers(assembly, extensionIdentifier: assemblyFilePath)
                        .ToImmutableDictionary(h => h.Name, h => h);

                    assemblyHandlers = new AssemblyHandlers()
                    {
                        WorkspaceMessageHandlers = messageWorkspaceHandlers,
                        DocumentMessageHandlers = messageDocumentHandlers,
                    };

                    // We don't add assemblyHandlers to _assemblies here and instead let _extensionMessageHandlerService.RegisterAssembly do it
                    // since RegisterAssembly can still fail if there are duplicated handler names.
                }
                catch (Exception e) when (FatalError.ReportAndPropagate(exception = e, ErrorSeverity.General))
                {
                    throw ExceptionUtilities.Unreachable();
                }
                finally
                {
                    // In case of exception, we cache null so that we don't try to load the same assembly again.
                    await _extensionMessageHandlerService.RegisterAssemblyAsync(
                        this, assemblyFileName, exception is null ? assemblyHandlers : null, cancellationToken).ConfigureAwait(false);
                }

                // The return is here, after RegisterAssembly, since RegisterAssembly can also throw an exception: the registration is not
                // completed until RegisterAssembly returns.
                return new(
                    [.. assemblyHandlers.WorkspaceMessageHandlers.Keys],
                    [.. assemblyHandlers.DocumentMessageHandlers.Keys]);
            }
        }

        private void EnsureGlobalLockIsOwned()
        {
            Contract.ThrowIfTrue(_extensionMessageHandlerService._lock.CurrentCount != 0, "Global lock should be owned");
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
