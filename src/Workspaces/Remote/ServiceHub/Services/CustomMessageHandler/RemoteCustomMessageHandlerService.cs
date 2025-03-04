// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CustomMessageHandler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteCustomMessageHandlerService : BrokeredServiceBase, IRemoteCustomMessageHandlerService
{
#if !NETSTANDARD2_0
    /// <summary>
    /// Extensions assembly load contexts and loaded handlers, indexed by handler file path. The handlers are indexed by type name.
    /// </summary>
    private readonly Dictionary<string, CustomMessageHandlerExtension> _extensions = new();

    private readonly System.Runtime.Loader.AssemblyLoadContext _defaultLoadContext
        = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(RemoteCustomMessageHandlerService).Assembly)
        ?? throw new InvalidOperationException($"Cannot get assembly load context for {nameof(RemoteCustomMessageHandlerService)}.");

    private readonly object _lockObject = new();
#endif

    internal sealed class Factory : FactoryBase<IRemoteCustomMessageHandlerService>
    {
        protected override IRemoteCustomMessageHandlerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteCustomMessageHandlerService(arguments);
    }

    public RemoteCustomMessageHandlerService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<string> HandleCustomMessageAsync(
        Checksum solutionChecksum,
        string assemblyPath,
        string typeFullName,
        string jsonMessage,
        DocumentId? documentId,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        throw new InvalidOperationException("Custom handlers are only supported in the servicehub host");
#else

#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        Requires.NotNullOrEmpty(assemblyPath);
        Requires.NotNullOrEmpty(typeFullName);
        Requires.NotNullOrEmpty(jsonMessage);

        var directory = Path.GetDirectoryName(assemblyPath);
        Contract.ThrowIfNull(directory);

        return RunServiceAsync(solutionChecksum, async solution =>
        {
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
                    if (!_extensions.TryGetValue(assemblyPath, out var extension))
                    {
                        var loadContext = new System.Runtime.Loader.AssemblyLoadContext(name: $"RemoteCustomMessageHandlerService assembly load context for {assemblyPath}", isCollectible: true);
                        loadContext.Resolving += ResolveExtensionAssembly;

                        extension = new CustomMessageHandlerExtension(loadContext);
                        _extensions[assemblyPath] = extension;
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

                // Deserialize the message into the expected TRequest type.
                var requestType = executeMethod.GetParameters()[0].ParameterType;
                var message = JsonSerializer.Deserialize(jsonMessage, requestType);

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
                var responseJson = JsonSerializer.Serialize(result, responseType);

                return responseJson;
            }
            catch (Exception e)
            {
                Log(TraceEventType.Error, $"Custom handler {assemblyPath} {typeFullName} error: {e}");
                throw;
            }
        }, cancellationToken);

        Assembly? ResolveExtensionAssembly(System.Runtime.Loader.AssemblyLoadContext context, AssemblyName assemblyName)
        {
            try
            {
                return _defaultLoadContext.LoadFromAssemblyName(assemblyName);
            }
            catch
            {
            }

            var extensionAssemblyPath = Path.Combine(directory, $"{assemblyName.Name}.dll");

            // This will throw FileNotFoundException if the assembly is not found.
            return context.LoadFromAssemblyPath(extensionAssemblyPath);
        }
#endif
    }

#pragma warning disable CA1822 // Mark members as static
    public ValueTask UnloadCustomMessageHandlerAsync(
        string assemblyPath,
        CancellationToken cancellationToken)
#pragma warning restore CA1822 // Mark members as static
    {
#if NETSTANDARD2_0
        throw new InvalidOperationException("Custom handlers are only supported in the servicehub host");
#else

#if DEBUG
        System.Diagnostics.Debugger.Launch();
#endif

        Requires.NotNullOrEmpty(assemblyPath);

        return RunServiceAsync((_) =>
        {
            try
            {
                lock (_lockObject)
                {
                    if (_extensions.TryGetValue(assemblyPath, out var extension))
                    {
                        _extensions.Remove(assemblyPath);
                        extension.AssemblyLoadContext.Unload();
                    }
                }
            }
            catch (Exception e)
            {
                Log(TraceEventType.Error, $"Error unloading {assemblyPath}: {e}");
                return ValueTask.FromException(e);
            }

            return ValueTask.CompletedTask;
        }, cancellationToken);
#endif
    }

#if !NETSTANDARD2_0
    public override void Dispose()
    {
        base.Dispose();

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
        public Dictionary<string, object> Handlers { get; } = new();
    }
#endif
}
