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
        System.Diagnostics.Debugger.Launch();

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

            // Create the Handler instance. Requires having a parameterless constructor.
            // ```
            // public class CustomMessageHandler
            // {
            //     public Task<TResponse> ExecuteAsync(TRequest, Document, CancellationToken);
            // }
            // ```
#if !NETSTANDARD2_0
            System.Runtime.Loader.AssemblyLoadContext? assemblyLoadContext = null;
#endif
            try
            {
#if NETSTANDARD2_0
                var assembly = Assembly.LoadFrom(assemblyPath);
#else
                var directory = Path.GetDirectoryName(assemblyPath);

                assemblyLoadContext = new(name: null, isCollectible: true);

                assemblyLoadContext.Resolving += (context, assemblyName) =>
                {
                    return assemblyName.Name switch
                    {
                        "Microsoft.CodeAnalysis" => typeof(AssemblyIdentity).Assembly,
                        "Microsoft.CodeAnalysis.Workspaces" => typeof(Document).Assembly,
                        _ => throw new InvalidOperationException()
                    };
                };

                var assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
#endif

                var type = assembly.GetType(typeFullName);
                var handler = Activator.CreateInstance(type);
                var executeMethod = type.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Instance);

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
                var resultTask = (Task)executeMethod.Invoke(handler, parameters);

                // Await the result and get its value.
                await resultTask.ConfigureAwait(false);
                var resultProperty = resultTask.GetType().GetProperty("Result");
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
#if !NETSTANDARD2_0
                assemblyLoadContext?.Unload();
#endif
            }
        }, cancellationToken);
    }
}
