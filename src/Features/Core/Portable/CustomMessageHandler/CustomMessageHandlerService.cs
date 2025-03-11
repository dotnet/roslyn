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

    private readonly System.Runtime.Loader.AssemblyLoadContext _defaultLoadContext
        = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(CustomMessageHandlerService).Assembly)
        ?? throw new InvalidOperationException($"Cannot get assembly load context for {nameof(CustomMessageHandlerService)}.");

    private readonly object _lockObject = new();
#endif

    private CustomMessageHandlerService()
    {
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
            object? handler;
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

                    // Create the Handler instance. Requires having a parameterless constructor.
                    // ```
                    // public class CustomMessageHandler
                    // {
                    //     public Task<TResponse> ExecuteAsync(TRequest, Solution, CancellationToken);
                    // }
                    //
                    // public class CustomMessageDocumentHandler
                    // {
                    //     public Task<TResponse> ExecuteAsync(TRequest, Document, CancellationToken);
                    // }
                    // ```
                    handler = Activator.CreateInstance(type)
                        ?? throw new InvalidOperationException($"Cannot create {typeFullName} from {assemblyPath}.");
                }
            }

            // TODO: use a well-known interface once available
            const string executeMethodName = "ExecuteAsync";
            var executeMethod = handler.GetType().GetMethod(executeMethodName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Cannot find method {executeMethodName} in type {typeFullName} assembly {assemblyPath}.");

            // CustomMessage.Message references positions in CustomMessage.TextDocument.
            // LinePositionConverter allows the serialization/deserialization of these indexes into LinePosition objects.
            JsonSerializerOptions jsonSerializerOptions = new();
            if (document is not null)
            {
                jsonSerializerOptions.Converters.Add(LinePositionConverter.Instance);
            }

            // Deserialize the message into the expected TRequest type.
            var requestType = executeMethod.GetParameters()[0].ParameterType;
            var message = JsonSerializer.Deserialize(jsonMessage, requestType, jsonSerializerOptions);

            // Invoke the execute method.
            object?[] parameters = document is null
                ? [message, solution, cancellationToken]
                : [message, document, cancellationToken];
            var resultTask = executeMethod.Invoke(handler, parameters) as Task
                ?? throw new InvalidOperationException($"Unexpected return type from {typeFullName}:{executeMethodName} in assembly {assemblyPath}, expected type Task<>.");

            // Await the result and get its value.
            await resultTask.ConfigureAwait(false);
            var resultProperty = resultTask.GetType().GetProperty(nameof(Task<>.Result))
                ?? throw new InvalidOperationException($"Unexpected return type from {typeFullName}:{executeMethodName} in assembly {assemblyPath}, expected type Task<>.");
            var result = resultProperty.GetValue(resultTask);

            // Serialize the TResponse and return it to the extension.
            var responseType = resultProperty.PropertyType;
            var responseJson = JsonSerializer.Serialize(result, responseType, jsonSerializerOptions);

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
        public Dictionary<string, object> Handlers { get; } = new();
    }
#endif
}
