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
    private readonly Dictionary<string, CustomMessageHandlerExtension> _extensions = new();

    /// <summary>
    /// Handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, IExtensionDocumentMessageHandlerWrapper> _documentHandlers = new();

    /// <summary>
    /// Handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, IExtensionWorspaceMessageHandlerWrapper> _handlers = new();

    private readonly IExtensionMessageHandlerFactory _customMessageHandlerFactory;

    // Used to protect access to _extensions, _handlers and _documentHandlers.
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

        CustomMessageHandlerExtension? extension;
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

                extension = new CustomMessageHandlerExtension(analyzerAssemblyLoader);
                _extensions[assemblyFolderPath] = extension;
            }
        }

        // AssemblyLoadLockObject is only used to avoid multiple calls from the same extensions to load the same assembly concurrently
        // resulting in the constructors of the same handlers being called more than once.
        // All other concurrent operations, including modifying extension.Assemblies are protected by _lockObject.
        lock (extension.AssemblyLoadLockObject)
        {
            if (extension.Assemblies.TryGetValue(assemblyFileName, out var extensionAssembly))
            {
                if (extensionAssembly.HasValue)
                {
                    return ValueTask.FromResult<RegisterExtensionResponse>(
                        new(extensionAssembly.Value.WorkspaceMessageHandlers.ToImmutableArray(), extensionAssembly.Value.DocumentMessageHandlers.ToImmutableArray()));
                }
                else
                {
                    throw new InvalidOperationException($"A previous attempt to load {assemblyFilePath} failed.");
                }
            }
            else
            {
                var mustCleanupExtension = false;
                try
                {
                    var assembly = extension.AnalyzerAssemblyLoader.LoadFromPath(assemblyFilePath);
                    var messageHandlers = _customMessageHandlerFactory.CreateWorkspaceMessageHandlers(assembly)
                        .ToDictionary(h => h.Name, h => h);
                    var messageDocumentHandlers = _customMessageHandlerFactory.CreateDocumentMessageHandlers(assembly)
                        .ToDictionary(h => h.Name, h => h);

                    // Important, you can lock _lockObject when holding a lock on AssemblyLoadLockObject, not vice-versa
                    lock (_lockObject)
                    {
                        // Make sure a call to UnloadCustomMessageHandlersAsync hasn't happened while we relinquished the lock on _lockObject
                        if (!_extensions.TryGetValue(assemblyFolderPath, out var currentExtension) || !currentExtension.Equals(extension))
                        {
                            // extension is not in the _extensions dictionary anymore, so it's AnalyzerAssemblyLoader must be unloaded
                            mustCleanupExtension = true;
                            throw new InvalidOperationException($"{assemblyFilePath} was unloaded while loading handlers.");
                        }

                        var duplicateHandler = _handlers.Keys.Intersect(messageHandlers.Keys).Concat(
                            _documentHandlers.Keys.Intersect(messageHandlers.Keys)).Concat(
                            _handlers.Keys.Intersect(messageDocumentHandlers.Keys)).Concat(
                            _documentHandlers.Keys.Intersect(messageDocumentHandlers.Keys)).FirstOrDefault();

                        if (duplicateHandler is not null)
                        {
                            throw new InvalidOperationException($"Handler name {duplicateHandler} is already registered.");
                        }

                        foreach (var handler in messageHandlers)
                        {
                            _handlers.Add(handler.Key, handler.Value);
                        }

                        foreach (var handler in messageDocumentHandlers)
                        {
                            _documentHandlers.Add(handler.Key, handler.Value);
                        }

                        extension.Assemblies[assemblyFileName] = new()
                        {
                            WorkspaceMessageHandlers = messageHandlers.Keys.ToImmutableHashSet(),
                            DocumentMessageHandlers = messageDocumentHandlers.Keys.ToImmutableHashSet(),
                        };

                        return ValueTask.FromResult<RegisterExtensionResponse>(
                            new(messageHandlers.Keys.ToImmutableArray(), messageDocumentHandlers.Keys.ToImmutableArray()));
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(
                        FunctionId.CustomMessageHandlerService_HandleCustomMessageAsync,
                        $"Error loading handlers from {assemblyFilePath}: {e}",
                        LogLevel.Error);

                    if (mustCleanupExtension)
                    {
                        extension.AnalyzerAssemblyLoader.Dispose();
                    }
                    else
                    {
                        // Cache null so that we don't try to load the same assembly again.
                        extension.Assemblies[assemblyFileName] = null;
                    }
                    throw;
                }
            }
        }
    }

    public async ValueTask<string> HandleExtensionWorkspaceMessageAsync(
        Solution solution,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
        IExtensionWorspaceMessageHandlerWrapper handler;
        lock (_lockObject)
        {
            if (!_handlers.TryGetValue(messageName, out handler!))
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

        try
        {
            CustomMessageHandlerExtension? extension = null;
            lock (_lockObject)
            {
                if (_extensions.TryGetValue(assemblyFolderPath, out extension))
                {
                    if (extension.Assemblies.TryGetValue(assemblyFileName, out var extensionAssembly))
                    {
                        extension.Assemblies.Remove(assemblyFileName);
                        UnregisterHandlersForAssembly(extensionAssembly);
                    }

                    if (extension.Assemblies.Count == 0)
                    {
                        _extensions.Remove(assemblyFolderPath);
                        foreach (var assembly in extension.Assemblies.Values)
                        {
                            UnregisterHandlersForAssembly(assembly);
                        }
                    }
                }
            }

            extension?.AnalyzerAssemblyLoader.Dispose();
        }
        catch (Exception e) when (LogAndPropagate(e))
        {
            // unreachable
        }

        return ValueTask.CompletedTask;

        bool LogAndPropagate(Exception e)
        {
            Logger.Log(
                FunctionId.CustomMessageHandlerService_UnloadCustomMessageHandlerAsync,
                $"Error unregistering {assemblyFilePath}: {e}",
                LogLevel.Error);
            return false;
        }

        void UnregisterHandlersForAssembly(CustomMessageHandlerAssembly? assembly)
        {
            if (assembly.HasValue)
            {
                foreach (var handler in assembly.Value.WorkspaceMessageHandlers)
                {
                    _handlers.Remove(handler);
                }

                foreach (var documentHandler in assembly.Value.DocumentMessageHandlers)
                {
                    _documentHandlers.Remove(documentHandler);
                }
            }
        }
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
        List<CustomMessageHandlerExtension> extensions;
        lock (_lockObject)
        {
            extensions = _extensions.Values.ToList();
            _extensions.Clear();
            _handlers.Clear();
            _documentHandlers.Clear();
        }

        foreach (var extension in extensions)
        {
            extension.AnalyzerAssemblyLoader.Dispose();
        }
    }

    private class CustomMessageHandlerExtension(IAnalyzerAssemblyLoaderInternal analyzerAssemblyLoader)
    {
        public IAnalyzerAssemblyLoaderInternal AnalyzerAssemblyLoader { get; } = analyzerAssemblyLoader;

        public Dictionary<string, CustomMessageHandlerAssembly?> Assemblies { get; } = new();

        /// <summary>
        /// Gets the object that is used to lock in order to avoid multiple calls from the same extensions to load the same assembly concurrently
        /// resulting in the constructors of the same handlers being called more than once.
        /// All other concurrent operations, including modifying <see cref="Assemblies"/> are protected by <see cref="_lockObject"/>.
        /// </summary>
        public object AssemblyLoadLockObject { get; } = new();
    }

    private readonly struct CustomMessageHandlerAssembly
    {
        /// <summary>
        /// Gets the names of the document-specific handlers that can be passed to <see cref="HandleExtensionDocumentMessageAsync"/>.
        /// </summary>
        public required ImmutableHashSet<string> DocumentMessageHandlers { get; init; }

        /// <summary>
        /// Gets the names of the non-document-specific handlers that can be passed to <see cref="HandleExtensionWorkspaceMessageAsync"/>.
        /// </summary>
        public required ImmutableHashSet<string> WorkspaceMessageHandlers { get; init; }
    }
}
#endif
