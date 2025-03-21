// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

[ExportWorkspaceService(typeof(ICustomMessageHandlerService)), Shared]
internal sealed class CustomMessageHandlerService : ICustomMessageHandlerService, IDisposable
{
    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by handler file path. The handlers are indexed by type name.
    /// </summary>
    private readonly Dictionary<string, CustomMessageHandlerExtension> _extensions = new();

    /// <summary>
    /// Handlers of document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, ICustomMessageDocumentHandlerWrapper> _documentHandlers = new();

    /// <summary>
    /// Handlers of non-document-related messages, indexed by handler message name.
    /// </summary>
    private readonly Dictionary<string, ICustomMessageHandlerWrapper> _handlers = new();

    private readonly ICustomMessageHandlerFactory _customMessageHandlerFactory;

    private readonly object _lockObject = new();

    private readonly AssemblyLoadContext _defaultLoadContext
        = AssemblyLoadContext.GetLoadContext(typeof(CustomMessageHandlerService).Assembly)
        ?? throw new InvalidOperationException($"Cannot get assembly load context for {nameof(CustomMessageHandlerService)}.");

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CustomMessageHandlerService(ICustomMessageHandlerFactory customMessageHandlerFactory)
    {
        _customMessageHandlerFactory = customMessageHandlerFactory;
    }

    public ValueTask<RegisterHandlersResponse> LoadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        string assemblyFileName,
        CancellationToken cancellationToken)
    {
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        var assemblyPath = Path.Combine(assemblyFolderPath, assemblyFileName);

        CustomMessageHandlerExtension extension;
        lock (_lockObject)
        {
            // Check if the assembly is already loaded.
            if (!_extensions.TryGetValue(assemblyFolderPath, out extension))
            {
                var loadContext = new AssemblyLoadContext(name: $"RemoteCustomMessageHandlerService assembly load context for {assemblyFolderPath}", isCollectible: true);
                loadContext.Resolving += ResolveExtensionAssembly;

                extension = new CustomMessageHandlerExtension(loadContext);
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
                    return ValueTask.FromResult<RegisterHandlersResponse>(
                        new(extensionAssembly.Value.Handlers.ToArray(), extensionAssembly.Value.DocumentHandlers.ToArray()));
                }
                else
                {
                    throw new InvalidOperationException($"A previous attempt to load {assemblyPath} failed.");
                }
            }
            else
            {
                var mustCleanupExtension = false;
                try
                {
                    var assembly = extension.AssemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
                    var messageHandlers = _customMessageHandlerFactory.CreateMessageHandlers(assembly)
                        .ToDictionary(h => h.Name, h => h);
                    var messageDocumentHandlers = _customMessageHandlerFactory.CreateMessageDocumentHandlers(assembly)
                        .ToDictionary(h => h.Name, h => h);

                    // Important, you can lock _lockObject when holding a lock on AssemblyLoadLockObject, not vice-versa
                    lock (_lockObject)
                    {
                        // Make sure a call to UnloadCustomMessageHandlersAsync hasn't happened while we relinquished the lock on _lockObject
                        if (!_extensions.TryGetValue(assemblyFolderPath, out var currentExtension) || !currentExtension.Equals(extension))
                        {
                            // extension is not in the _extensions dictionary anymore, so it's AssemblyLoadContext must be unloaded
                            mustCleanupExtension = true;
                            throw new InvalidOperationException($"{assemblyPath} was unloaded while loading handlers.");
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
                            Handlers = messageHandlers.Keys.ToHashSet(),
                            DocumentHandlers = messageDocumentHandlers.Keys.ToHashSet(),
                        };

                        return ValueTask.FromResult<RegisterHandlersResponse>(
                            new(messageHandlers.Keys.ToArray(), messageDocumentHandlers.Keys.ToArray()));
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(
                        FunctionId.CustomMessageHandlerService_HandleCustomMessageAsync,
                        $"Error loading handlers from {assemblyPath}: {e}",
                        LogLevel.Error);

                    if (mustCleanupExtension)
                    {
                        extension.AssemblyLoadContext.Unload();
                    }
                    else
                    {
                        extension.Assemblies[assemblyFileName] = null;
                    }
                    throw;
                }
            }
        }

        Assembly? ResolveExtensionAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            try
            {
                return _defaultLoadContext.LoadFromAssemblyName(assemblyName);
            }
            catch
            {
            }

            var extensionAssemblyPath = Path.Combine(assemblyFolderPath, $"{assemblyName.Name}.dll");

            // This will throw FileNotFoundException if the assembly is not found.
            return context.LoadFromAssemblyPath(extensionAssemblyPath);
        }
    }

    public async ValueTask<string> HandleCustomMessageAsync(
        Solution solution,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken)
    {
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        ICustomMessageHandlerWrapper handler;
        lock (_lockObject)
        {
            if (!_handlers.TryGetValue(messageName, out handler!))
            {
                throw new InvalidOperationException($"No handler found for message {messageName}.");
            }
        }

        var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
        var result = await handler.ExecuteAsync(message, solution, cancellationToken)
            .ConfigureAwait(false);
        var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);
        return responseJson;
    }

    public async ValueTask<string> HandleCustomDocumentMessageAsync(
        Solution solution,
        string messageName,
        string jsonMessage,
        DocumentId documentId,
        CancellationToken cancellationToken)
    {
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(document);

        ICustomMessageDocumentHandlerWrapper handler;
        lock (_lockObject)
        {
            if (!_documentHandlers.TryGetValue(messageName, out handler!))
            {
                throw new InvalidOperationException($"No document handler found for message {messageName}.");
            }
        }

        var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);
        var result = await handler.ExecuteAsync(message, document, solution, cancellationToken)
            .ConfigureAwait(false);
        var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);
        return responseJson;
    }

    public ValueTask UnloadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        CancellationToken cancellationToken)
    {
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        try
        {
            CustomMessageHandlerExtension extension = default;
            lock (_lockObject)
            {
                if (_extensions.TryGetValue(assemblyFolderPath, out extension))
                {
                    _extensions.Remove(assemblyFolderPath);

                    foreach (var assembly in extension.Assemblies)
                    {
                        if (assembly.Value.HasValue)
                        {
                            foreach (var handler in assembly.Value.Value.Handlers)
                            {
                                _handlers.Remove(handler);
                            }

                            foreach (var documentHandler in assembly.Value.Value.DocumentHandlers)
                            {
                                _documentHandlers.Remove(documentHandler);
                            }
                        }
                    }
                }
            }

            extension.AssemblyLoadContext?.Unload();
        }
        catch (Exception e) when (LogAndPropagate(e))
        {
            // unreachable
        }

        bool LogAndPropagate(Exception e)
        {
            Logger.Log(
                FunctionId.CustomMessageHandlerService_UnloadCustomMessageHandlerAsync,
                $"Error unloading {assemblyFolderPath}: {e}",
                LogLevel.Error);
            return false;
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
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
            extension.AssemblyLoadContext.Unload();
        }
    }

    private readonly struct CustomMessageHandlerExtension(AssemblyLoadContext assemblyLoadContext) : IEquatable<CustomMessageHandlerExtension>
    {
        public AssemblyLoadContext AssemblyLoadContext { get; } = assemblyLoadContext;

        public Dictionary<string, CustomMessageHandlerAssembly?> Assemblies { get; } = new();

        public object AssemblyLoadLockObject { get; } = new();

        // Used to avoid race conditions between RegisterHandlersAsync and UnloadCustomMessageHandlersAsync
        public bool Equals(CustomMessageHandlerExtension other)
            => ReferenceEquals(Assemblies, other.Assemblies);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is CustomMessageHandlerExtension other && Equals(other);

        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(Assemblies);
    }

    private readonly struct CustomMessageHandlerAssembly
    {
        public required HashSet<string> DocumentHandlers { get; init; }

        public required HashSet<string> Handlers { get; init; }
    }
}
#endif
