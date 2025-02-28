// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CustomMessageHandler;
using Microsoft.CodeAnalysis.Remote.CustomMessageHandler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteCustomMessageHandlerService : BrokeredServiceBase, IRemoteCustomMessageHandlerService
{
    internal sealed class Factory : FactoryBase<IRemoteCustomMessageHandlerService>
    {
        protected override IRemoteCustomMessageHandlerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteCustomMessageHandlerService(arguments);
    }

    public RemoteCustomMessageHandlerService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<HandleCustomMessageResponse> HandleCustomMessageAsync(
        Checksum solutionChecksum,
        string assemblyPath,
        string typeFullName,
        string jsonMessage,
        DocumentId? documentId,
        LinePosition[] positions,
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
        Requires.NotNullOrEmpty(positions);

        return RunServiceAsync(solutionChecksum, async solution =>
        {
            Document? document = null;
            if (documentId is not null)
            {
                document = solution.GetDocument(documentId) ?? await solution.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(document);
            }

            System.Runtime.Loader.AssemblyLoadContext? assemblyLoadContext = null;
            try
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                Contract.ThrowIfNull(directory);

                var defaultLoadContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(RemoteCustomMessageHandlerService).Assembly);
                Contract.ThrowIfNull(defaultLoadContext);

                assemblyLoadContext = new System.Runtime.Loader.AssemblyLoadContext(name: $"RemoteCustomMessageHandlerService assembly load context for {directory}", isCollectible: true);
                assemblyLoadContext.Resolving += (context, assemblyName) =>
                {
                    var sharedAssembly = defaultLoadContext.Assemblies.Where(a => a.GetName().Name == assemblyName.Name).FirstOrDefault();

                    if (sharedAssembly is not null)
                    {
                        if (sharedAssembly.GetName().Version < assemblyName.Version)
                        {
                            throw new InvalidOperationException($"The version of the loaded assembly {assemblyName.Name} is too low: requested {assemblyName.Version}, found {sharedAssembly.GetName().Version}.");
                        }

                        return sharedAssembly;
                    }

                    var extensionAssemblyPath = Path.Combine(directory, $"{assemblyName.Name}.dll");
                    if (File.Exists(extensionAssemblyPath))
                    {
                        return assemblyLoadContext.LoadFromAssemblyPath(extensionAssemblyPath);
                    }

                    return null;
                };

                var assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

                var type = assembly.GetType(typeFullName)
                    ?? throw new InvalidOperationException($"Cannot find type {typeFullName} in {assemblyPath}.");

                // Create the Handler instance. Requires having a parameterless constructor.
                // ```
                // public class CustomMessageHandler
                // {
                //     public Task<TResponse> ExecuteAsync(TRequest, Document, CancellationToken);
                // }
                // ```
                var handler = Activator.CreateInstance(type);
                const string executeMethodName = "ExecuteAsync";
                var executeMethod = type.GetMethod(executeMethodName, BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new InvalidOperationException($"Cannot find method {executeMethodName} in type {typeFullName} assembly {assemblyPath}.");

                // CustomMessage.Message references positions in CustomMessage.TextDocument as indexes referencing CustomMessage.Positions.
                // LinePositionReadConverter allows the deserialization of these indexes into LinePosition objects.
                JsonSerializerOptions readOptions = new();
                LinePositionReadConverter linePositionReadConverter = new(positions);
                readOptions.Converters.Add(linePositionReadConverter);

                // Deserialize the message into the expected TRequest type.
                var requestType = executeMethod.GetParameters()[0].ParameterType;
                var message = JsonSerializer.Deserialize(jsonMessage, requestType, readOptions);

                // Invoke the execute method.
                var parameters = new object?[] { message, document, cancellationToken };
                var resultTask = executeMethod.Invoke(handler, parameters) as Task
                    ?? throw new InvalidOperationException($"Unexpected return type from {typeFullName}:{executeMethodName} in assembly {assemblyPath}, expected type Task<>.");

                // Await the result and get its value.
                await resultTask.ConfigureAwait(false);
                var resultProperty = resultTask.GetType().GetProperty(nameof(Task<>.Result))
                    ?? throw new InvalidOperationException($"Unexpected return type from {typeFullName}:{executeMethodName} in assembly {assemblyPath}, expected type Task<>.");
                var result = resultProperty.GetValue(resultTask);

                // CustomResponse.Message must express positions in CustomMessage.TextDocument as indexes referencing CustomResponse.Positions.
                // LinePositionWriteConverter allows serializing extender-defined types into json with indexes referencing LinePosition objects.
                JsonSerializerOptions writeOptions = new();
                LinePositionWriteConverter linePositionWriteConverter = new();
                writeOptions.Converters.Add(linePositionWriteConverter);

                // Serialize the TResponse and return it to the extension.
                var responseType = resultProperty.PropertyType;
                var responseJson = JsonSerializer.Serialize(result, responseType, writeOptions);

                return new HandleCustomMessageResponse()
                {
                    Message = responseJson,
                    Positions = linePositionWriteConverter.LinePositions.OrderBy(lp => lp.Value).Select(lp => lp.Key).ToArray(),
                };
            }
            finally
            {
                assemblyLoadContext?.Unload();
            }
        }, cancellationToken);
#endif
    }
}
