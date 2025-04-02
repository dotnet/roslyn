// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETSTANDARD2_0

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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Extensions;

[ExportWorkspaceService(typeof(IExtensionMessageHandlerService)), Shared]
internal sealed class ExtensionMessageHandlerService : IExtensionMessageHandlerService, IDisposable
{
    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by handler file path. The handlers are indexed by type name.
    /// </summary>
    private readonly Dictionary<string, Extension> _extensions = new();

    /// <summary>
    /// Handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, IExtensionDocumentMessageHandlerWrapper> _documentHandlers = new();

    /// <summary>
    /// Handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, IExtensionWorkspaceMessageHandlerWrapper> _workspaceHandlers = new();

    private readonly IExtensionMessageHandlerFactory _customMessageHandlerFactory;

    // Used to protect access to _extensions, _handlers, _documentHandlers and CustomMessageHandlerExtension.Assemblies.
    private readonly object _lockObject = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExtensionMessageHandlerService(IExtensionMessageHandlerFactory customMessageHandlerFactory)
    {
        _customMessageHandlerFactory = customMessageHandlerFactory;
    }

    public ValueTask<RegisterExtensionResponse> RegisterExtensionAsync(
        Solution solution,
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var assemblyFileName = Path.GetFileName(assemblyFilePath);
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        var analyzerAssemblyLoaderProvider = solution.Services.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        Extension? extension;
        lock (_lockObject)
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

                return ValueTask.FromResult<RegisterExtensionResponse>(new(
                    assemblyHandlers.WorkspaceMessageHandlers.Keys.ToImmutableArray(),
                    assemblyHandlers.DocumentMessageHandlers.Keys.ToImmutableArray()));
            }
        }

        return extension.LoadAssemblyAsync(assemblyFileName);
    }

    public async ValueTask<string> HandleExtensionWorkspaceMessageAsync(
        Solution solution,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
        IExtensionWorkspaceMessageHandlerWrapper handler;
        lock (_lockObject)
        {
            if (!_workspaceHandlers.TryGetValue(messageName, out handler!))
            {
                throw new InvalidOperationException($"No handler found for message {messageName}.");
            }
        }

        // Any exception thrown in this method is left to bubble up to the extension.
        var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
        var result = await handler.ExecuteAsync(message, solution, cancellationToken)
            .ConfigureAwait(false);
        var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);
        return responseJson;
    }

    public async ValueTask<string> HandleExtensionDocumentMessageAsync(
        Document document,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
        IExtensionDocumentMessageHandlerWrapper handler;
        lock (_lockObject)
        {
            if (!_documentHandlers.TryGetValue(messageName, out handler!))
            {
                throw new InvalidOperationException($"No document handler found for message {messageName}.");
            }
        }

        // Any exception thrown in this method is left to bubble up to the extension.
        var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
        var result = await handler.ExecuteAsync(message, document, cancellationToken)
            .ConfigureAwait(false);
        var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);
        return responseJson;
    }

    public ValueTask UnregisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken)
    {
        var assemblyFileName = Path.GetFileName(assemblyFilePath);
        var assemblyFolderPath = Path.GetDirectoryName(assemblyFilePath)
            ?? throw new InvalidOperationException($"Unable to get the directory name for {assemblyFilePath}.");

        Extension? extension = null;
        lock (_lockObject)
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
                {
                    return ValueTask.CompletedTask;
                }

                _extensions.Remove(assemblyFolderPath);
            }
        }

        extension?.AnalyzerAssemblyLoader.Dispose();

        return ValueTask.CompletedTask;
    }

    public ValueTask ResetAsync(CancellationToken cancellationToken)
    {
        Clear();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
        => Clear();

    private void Clear()
    {
        List<Extension> extensions;
        lock (_lockObject)
        {
            extensions = _extensions.Values.ToList();
            _extensions.Clear();
            _workspaceHandlers.Clear();
            _documentHandlers.Clear();
        }

        foreach (var extension in extensions)
        {
            extension.AnalyzerAssemblyLoader.Dispose();
        }
    }

    private void RegisterAssembly(Extension extension, string assemblyFileName, AssemblyHandlers? assemblyHandlers)
    {
        lock (_lockObject)
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

    private class Extension(ExtensionMessageHandlerService extensionMessageHandlerService, IAnalyzerAssemblyLoaderInternal analyzerAssemblyLoader, string assemblyFolderPath)
    {
        /// <summary>
        /// Gets the object that is used to lock in order to avoid multiple calls from the same extensions to load the same assembly concurrently
        /// resulting in the constructors of the same handlers being called more than once.
        /// All other concurrent operations, including modifying <see cref="_assemblies"/> are protected by
        /// <see cref="ExtensionMessageHandlerService._lockObject"/>.
        /// </summary>
        private readonly object _assemblyLoadLockObject = new();

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

        public ValueTask<RegisterExtensionResponse> LoadAssemblyAsync(string assemblyFileName)
        {
            var assemblyFilePath = Path.Combine(AssemblyFolderPath, assemblyFileName);

            // _extensionMessageHandlerService.RegisterAssembly locks _lockObject.
            // You can lock _lockObject when holding a lock on AssemblyLoadLockObject, not vice-versa
            if (Monitor.IsEntered(_extensionMessageHandlerService._lockObject))
            {
                throw new InvalidOperationException("Global lock should not be owned");
            }

            // AssemblyLoadLockObject is only used to avoid multiple calls from the same extensions to load the same assembly concurrently
            // resulting in the constructors of the same handlers being called more than once.
            // All other concurrent operations, including modifying extension.Assemblies are protected by _lockObject.
            lock (_assemblyLoadLockObject)
            {
                AssemblyHandlers? assemblyHandlers = null;

                try
                {
                    var assembly = AnalyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                    var messageWorkspaceHandlers = _extensionMessageHandlerService._customMessageHandlerFactory.CreateWorkspaceMessageHandlers(assembly)
                        .ToImmutableDictionary(h => h.Name, h => h);
                    var messageDocumentHandlers = _extensionMessageHandlerService._customMessageHandlerFactory.CreateDocumentMessageHandlers(assembly)
                        .ToImmutableDictionary(h => h.Name, h => h);

                    assemblyHandlers = new AssemblyHandlers()
                    {
                        WorkspaceMessageHandlers = messageWorkspaceHandlers,
                        DocumentMessageHandlers = messageDocumentHandlers,
                    };

                    // We don't add assemblyHandlers to _assemblies here and instead let _extensionMessageHandlerService.RegisterAssembly do it
                    // since RegisterAssembly can still fail if there are duplicated handler names.
                }
                catch (Exception e) when (LogAndPropagate(e))
                {
                    // unreachable
                }
                finally
                {
                    // In case of exception, we cache null so that we don't try to load the same assembly again.
                    _extensionMessageHandlerService.RegisterAssembly(this, assemblyFileName, assemblyHandlers);
                }

                bool LogAndPropagate(Exception e)
                {
                    Logger.Log(
                        FunctionId.CustomMessageHandlerService_HandleCustomMessageAsync,
                        $"Error loading handlers from {assemblyFilePath}: {e}",
                        LogLevel.Error);
                    return false;
                }

                return ValueTask.FromResult<RegisterExtensionResponse>(new(
                    assemblyHandlers!.WorkspaceMessageHandlers.Keys.ToImmutableArray(),
                    assemblyHandlers.DocumentMessageHandlers.Keys.ToImmutableArray()));
            }
        }

        private void EnsureGlobalLockIsOwned()
        {
            if (!Monitor.IsEntered(_extensionMessageHandlerService._lockObject))
            {
                throw new InvalidOperationException("Global lock should be owned");
            }
        }
    }

    private class AssemblyHandlers
    {
        /// <summary>
        /// Gets the document-specific handlers that can be passed to <see cref="HandleExtensionDocumentMessageAsync"/>, indexed by their name.
        /// </summary>
        public required ImmutableDictionary<string, IExtensionDocumentMessageHandlerWrapper> DocumentMessageHandlers { get; init; }

        /// <summary>
        /// Gets the non-document-specific handlers that can be passed to <see cref="HandleExtensionWorkspaceMessageAsync"/>, indexed by their name.
        /// </summary>
        public required ImmutableDictionary<string, IExtensionWorkspaceMessageHandlerWrapper> WorkspaceMessageHandlers { get; init; }
    }
}
#endif
