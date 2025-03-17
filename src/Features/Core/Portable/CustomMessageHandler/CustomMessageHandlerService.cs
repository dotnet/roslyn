// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
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

    private readonly ICustomMessageHandlerFactory _customMessageHandlerFactory;

    private readonly AssemblyLoadContext _defaultLoadContext
        = AssemblyLoadContext.GetLoadContext(typeof(CustomMessageHandlerService).Assembly)
        ?? throw new InvalidOperationException($"Cannot get assembly load context for {nameof(CustomMessageHandlerService)}.");

    private readonly object _lockObject = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CustomMessageHandlerService(ICustomMessageHandlerFactory customMessageHandlerFactory)
    {
        _customMessageHandlerFactory = customMessageHandlerFactory;
    }

    public async ValueTask<string> HandleCustomMessageAsync(
        Solution solution,
        string assemblyFolderPath,
        string assemblyFileName,
        string typeFullName,
        string jsonMessage,
        DocumentId? documentId,
        CancellationToken cancellationToken)
    {
#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        var assemblyPath = Path.Combine(assemblyFolderPath, assemblyFileName);

        Document? document = null;
        if (documentId is not null)
        {
            document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(document);
        }

        ICustomMessageHandlerWrapper? handler;
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

        lock (extension.Handlers)
        {
            if (!extension.Handlers.TryGetValue(typeFullName, out handler))
            {
                try
                {
                    var assembly = extension.AssemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
                    var type = assembly.GetType(typeFullName)
                        ?? throw new InvalidOperationException($"Cannot find type {typeFullName} in {assemblyPath}.");

                    handler = _customMessageHandlerFactory.Create(type);
                    extension.Handlers[typeFullName] = handler;
                }
                catch (Exception e)
                {
                    Logger.Log(
                        FunctionId.CustomMessageHandlerService_HandleCustomMessageAsync,
                        $"Error creating custom handler {assemblyPath} {typeFullName}: {e}",
                        LogLevel.Error);

                    extension.Handlers[typeFullName] = null;
                    throw;
                }
            }
            else if (handler is null)
            {
                throw new InvalidOperationException($"A previous attempt to instantiate {typeFullName} in {assemblyPath} failed.");
            }
        }

        var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);

        var result = await handler.ExecuteAsync(message, document, solution, cancellationToken)
            .ConfigureAwait(false);

        var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);

        return responseJson;

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
        }

        foreach (var extension in extensions)
        {
            extension.Handlers.Clear();
            extension.AssemblyLoadContext.Unload();
        }
    }

    private readonly struct CustomMessageHandlerExtension(AssemblyLoadContext assemblyLoadContext)
    {
        public AssemblyLoadContext AssemblyLoadContext { get; } = assemblyLoadContext;

        public Dictionary<string, ICustomMessageHandlerWrapper?> Handlers { get; } = new();
    }
}
#endif
