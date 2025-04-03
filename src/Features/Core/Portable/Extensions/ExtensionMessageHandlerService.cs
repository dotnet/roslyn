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
    private readonly Dictionary<string, Extension> _extensions = new();

    /// <summary>
    /// Handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, IExtensionMessageHandlerWrapper<Document>> _documentHandlers = new();

    /// <summary>
    /// Handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, IExtensionMessageHandlerWrapper<Solution>> _workspaceHandlers = new();

    // Used to protect access to _extensions, _handlers, _documentHandlers and Extension._assemblies.
    private readonly SemaphoreSlim _lock = new(initialCount: 1);

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

        var analyzerAssemblyLoaderProvider = _solutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
        Extension? extension;
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            // Check if the assembly is already loaded.
            if (!_extensions.TryGetValue(assemblyFolderPath, out extension))
            {
                var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();

                // Allow this assembly loader to load any dll in assemblyFolderPath.
                foreach (var dll in Directory.EnumerateFiles(assemblyFolderPath, "*.dll"))
                {
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

                extension = new Extension(this, analyzerAssemblyLoader, assemblyFolderPath);
                _extensions[assemblyFolderPath] = extension;
            }

            if (extension.TryGetAssemblyHandlers(assemblyFileName, out var assemblyHandlers))
            {
                if (assemblyHandlers is null)
                {
                    throw new InvalidOperationException($"A previous attempt to load {assemblyFilePath} failed.");
                }

                return new(
                    [.. assemblyHandlers.WorkspaceMessageHandlers.Keys],
                    [.. assemblyHandlers.DocumentMessageHandlers.Keys]);
            }
        }

        // Intentionally call this outside of the lock.
        return await extension.LoadAssemblyAsync(assemblyFileName).ConfigureAwait(false);
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

        Extension? extension = null;
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_extensions.TryGetValue(assemblyFolderPath, out extension))
            {
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

    private async ValueTask<VoidResult> ResetInCurrentProcessAsync(CancellationToken cancellationToken)
    {
        List<Extension> extensions;
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            extensions = [.. _extensions.Values];
            _extensions.Clear();
            _workspaceHandlers.Clear();
            _documentHandlers.Clear();
        }

        foreach (var extension in extensions)
            extension.AnalyzerAssemblyLoader.Dispose();

        return default;
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

    private ValueTask<string> HandleExtensionWorkspaceMessageInCurrentProcessAsync(Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken)
        => HandleExtensionMessageAsync(solution, messageName, jsonMessage, _workspaceHandlers, cancellationToken);

    public ValueTask<string> HandleExtensionDocumentMessageInCurrentProcessAsync(Document document, string messageName, string jsonMessage, CancellationToken cancellationToken)
        => HandleExtensionMessageAsync(document, messageName, jsonMessage, _documentHandlers, cancellationToken);

    private async ValueTask<string> HandleExtensionMessageAsync<TArgument>(
        TArgument argument,
        string messageName,
        string jsonMessage,
        Dictionary<string, IExtensionMessageHandlerWrapper<TArgument>> handlers,
        CancellationToken cancellationToken)
    {
        IExtensionMessageHandlerWrapper<TArgument> handler;
        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            // handlers here is either _workspaceHandlers or _documentHandlers, so it must be protected
            // by _lockObject.
            if (!handlers.TryGetValue(messageName, out handler!))
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
            if (!_extensions.TryGetValue(extension.AssemblyFolderPath, out var currentExtension) || !currentExtension.Equals(extension))
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

    private sealed class Extension(ExtensionMessageHandlerService extensionMessageHandlerService, IAnalyzerAssemblyLoaderInternal analyzerAssemblyLoader, string assemblyFolderPath)
    {
        /// <summary>
        /// Gets the object that is used to lock in order to avoid multiple calls from the same extensions to load the
        /// same assembly concurrently resulting in the constructors of the same handlers being called more than once.
        /// All other concurrent operations, including modifying <see cref="_assemblies"/> are protected by <see
        /// cref="_lock"/>.
        /// </summary>
        private readonly SemaphoreSlim _assemblyLoadLock = new(initialCount: 1);

        private readonly Dictionary<string, AssemblyHandlers?> _assemblies = new();

        private readonly ExtensionMessageHandlerService _extensionMessageHandlerService = extensionMessageHandlerService;

        public IAnalyzerAssemblyLoaderInternal AnalyzerAssemblyLoader { get; } = analyzerAssemblyLoader;

        public string AssemblyFolderPath { get; } = assemblyFolderPath;

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

                try
                {
                    var assembly = AnalyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
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
                catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.General))
                {
                    // unreachable
                }
                finally
                {
                    // In case of exception, we cache null so that we don't try to load the same assembly again.
                    await _extensionMessageHandlerService.RegisterAssemblyAsync(
                        this, assemblyFileName, assemblyHandlers, cancellationToken).ConfigureAwait(false);
                }

                return new(
                    [.. assemblyHandlers!.WorkspaceMessageHandlers.Keys],
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
