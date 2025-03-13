// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

[Export(typeof(ICustomMessageHandlerService)), Shared]
internal sealed class CustomMessageHandlerService : ICustomMessageHandlerService, IDisposable
{
    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by handler file path. The handlers are indexed by type name.
    /// </summary>
    private readonly Dictionary<string, CustomMessageHandlerExtension> _extensions = new();

    private readonly ICustomMessageHandlerFactory _customMessageHandlerFactory;

    private readonly System.Runtime.Loader.AssemblyLoadContext _defaultLoadContext
        = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(CustomMessageHandlerService).Assembly)
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

        _ = assemblyFolderPath ?? throw new ArgumentNullException(nameof(assemblyFolderPath));
        _ = assemblyFileName ?? throw new ArgumentNullException(nameof(assemblyFileName));
        _ = typeFullName ?? throw new ArgumentNullException(nameof(typeFullName));
        _ = jsonMessage ?? throw new ArgumentNullException(nameof(jsonMessage));

        var assemblyPath = Path.Combine(assemblyFolderPath, assemblyFileName);

        Document? document = null;
        if (documentId is not null)
        {
            document = solution.GetDocument(documentId) ?? await solution.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(document);
        }

        try
        {
            ICustomMessageHandlerWrapper? handler;
            lock (_lockObject)
            {
                // Check if the assembly is already loaded.
                if (!_extensions.TryGetValue(assemblyFolderPath, out var extension))
                {
                    var loadContext = new System.Runtime.Loader.AssemblyLoadContext(name: $"RemoteCustomMessageHandlerService assembly load context for {assemblyFolderPath}", isCollectible: true);
                    loadContext.Resolving += ResolveExtensionAssembly;

                    extension = new CustomMessageHandlerExtension(loadContext);
                    _extensions[assemblyFolderPath] = extension;
                }

                if (!extension.Handlers.TryGetValue(typeFullName, out handler))
                {
                    var assembly = extension.AssemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
                    var type = assembly.GetType(typeFullName)
                        ?? throw new InvalidOperationException($"Cannot find type {typeFullName} in {assemblyPath}.");

                    handler = _customMessageHandlerFactory.Create(type);
                }
            }

            var message = JsonSerializer.Deserialize(jsonMessage, handler.MessageType);

            var result = await handler.ExecuteAsync(message, document, solution, cancellationToken)
                .ConfigureAwait(false);

            var responseJson = JsonSerializer.Serialize(result, handler.ResponseType);

            return responseJson;
        }
        catch (Exception e)
        {
            Logger.Log(
                FunctionId.CustomMessageHandlerService_HandleCustomMessageAsync,
                $"Custom handler {assemblyPath} {typeFullName} error: {e}",
                LogLevel.Error);
            throw;
        }

        Assembly? ResolveExtensionAssembly(System.Runtime.Loader.AssemblyLoadContext context, AssemblyName assemblyName)
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

        _ = assemblyFolderPath ?? throw new ArgumentNullException(nameof(assemblyFolderPath));

        try
        {
            lock (_lockObject)
            {
                if (_extensions.TryGetValue(assemblyFolderPath, out var extension))
                {
                    _extensions.Remove(assemblyFolderPath);
                    extension.AssemblyLoadContext.Unload();
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(
                FunctionId.CustomMessageHandlerService_UnloadCustomMessageHandlerAsync,
                $"Error unloading {assemblyFolderPath}: {e}",
                LogLevel.Error);
            return ValueTask.FromException(e);
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            foreach (var extension in _extensions.Values)
            {
                extension.Handlers.Clear();
                extension.AssemblyLoadContext.Unload();
            }

            _extensions.Clear();
        }
    }

    private record struct CustomMessageHandlerExtension(System.Runtime.Loader.AssemblyLoadContext AssemblyLoadContext)
    {
        public Dictionary<string, ICustomMessageHandlerWrapper> Handlers { get; } = new();
    }
}
#endif
