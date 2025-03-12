// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal sealed class CustomMessageHandlerService : IDisposable
{
#if !NETSTANDARD2_0
    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by handler file path. The handlers are indexed by type name.
    /// </summary>
    private readonly Dictionary<string, CustomMessageHandlerExtension> _extensions = new();

    private readonly ICustomMessageHandlerFactory customMessageHandlerFactory;

    private readonly System.Runtime.Loader.AssemblyLoadContext _defaultLoadContext
        = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(CustomMessageHandlerService).Assembly)
        ?? throw new InvalidOperationException($"Cannot get assembly load context for {nameof(CustomMessageHandlerService)}.");

    private readonly object _lockObject = new();
#endif

    private CustomMessageHandlerService()
    {
#if !NETSTANDARD2_0
        // TODO use dependency injection instead
        var externalAccessAssembly = Assembly.Load("Microsoft.CodeAnalysis.ExternalAccess.CustomMessage")
            ?? throw new InvalidOperationException($"Cannot load Microsoft.CodeAnalysis.ExternalAccess.CustomMessage.dll");
        var customMessageHandlerFactoryType = externalAccessAssembly.GetType("Microsoft.CodeAnalysis.CustomMessageHandler.CustomMessageHandlerFactory")
            ?? throw new InvalidOperationException($"Cannot find Microsoft.CodeAnalysis.CustomMessageHandler.CustomMessageHandlerFactory type");
        customMessageHandlerFactory = Activator.CreateInstance(customMessageHandlerFactoryType) as ICustomMessageHandlerFactory
            ?? throw new InvalidOperationException($"Cannot instantiate Microsoft.CodeAnalysis.CustomMessageHandler.CustomMessageHandlerFactory");
#endif
    }

    public static Lazy<CustomMessageHandlerService> Instance { get; } = new(
        () =>
        {
#if NETSTANDARD2_0
            throw new InvalidOperationException("Custom message handlers are not supported in .NET Standard 2.0.");
#else
            return new CustomMessageHandlerService();
#endif
        },
        isThreadSafe: true);

#pragma warning disable CA1822, CS1998 // Mark members as static, Async method lacks 'await' operators and will run synchronously
    public async ValueTask<string> HandleCustomMessageAsync(
#pragma warning restore CA1822, CS1998 // Mark members as static, Async method lacks 'await' operators and will run synchronously
        Solution solution,
        string assemblyFolderPath,
        string assemblyFileName,
        string typeFullName,
        string jsonMessage,
        DocumentId? documentId,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        throw new InvalidOperationException("Custom message handlers are not supported in .NET Standard 2.0.");
#else
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

                    handler = customMessageHandlerFactory.Create(type);
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
#endif
    }

#pragma warning disable CA1822 // Mark members as static
    public ValueTask UnloadCustomMessageHandlersAsync(
#pragma warning restore CA1822 // Mark members as static
        string assemblyFolderPath,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        throw new InvalidOperationException("Custom message handlers are not supported in .NET Standard 2.0.");
#else
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
#endif
    }

    public void Dispose()
    {
#if !NETSTANDARD2_0
        lock (_lockObject)
        {
            foreach (var extension in _extensions.Values)
            {
                extension.Handlers.Clear();
                extension.AssemblyLoadContext.Unload();
            }

            _extensions.Clear();
        }
#endif
    }

#if !NETSTANDARD2_0
    private record struct CustomMessageHandlerExtension(System.Runtime.Loader.AssemblyLoadContext AssemblyLoadContext)
    {
        public Dictionary<string, ICustomMessageHandlerWrapper> Handlers { get; } = new();
    }
#endif
}
