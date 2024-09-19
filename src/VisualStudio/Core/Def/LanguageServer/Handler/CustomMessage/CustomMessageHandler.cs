// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageHandler()
    : ILspServiceDocumentRequestHandler<CustomMessageParams, CustomResponse>
{
    private const string MethodName = "roslyn/customMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CustomMessageParams request)
    {
        return request.Message.TextDocument;
    }

    public async Task<CustomResponse> HandleRequestAsync(CustomMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        // Create the Handler instance. Requires having a parameterless constructor.
        // ```
        // public class CustomMessageHandler
        // {
        //     public Task<TResponse> ExecuteAsync(TRequest, Document, CancellationToken);
        // }
        // ```
        var handler = Activator.CreateInstanceFrom(request.AssemblyPath, request.TypeFullName).Unwrap();

        // Use reflection to find the ExecuteAsync method.
        var handlerType = handler.GetType();
        var executeMethod = handlerType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Instance);

        // Deserialize the message into the expected TRequest type.
        var requestType = executeMethod.GetParameters()[0].ParameterType;
        var message = JsonSerializer.Deserialize(request.Message.Message, requestType);

        // Invoke the execute method.
        var parameters = new object?[] { message, context.Document, cancellationToken };
        var resultTask = (Task)executeMethod.Invoke(handler, parameters);

        // Await the result and get its value.
        await resultTask.ConfigureAwait(false);
        var resultProperty = resultTask.GetType().GetProperty("Result");
        var result = resultProperty.GetValue(resultTask);

        // Serialize the TResponse and return it to the extension.
        var responseType = resultProperty.PropertyType;
        var responseJson = JsonSerializer.Serialize(result, responseType);
        return new CustomMessage(JsonNode.Parse(responseJson)!, request.Message.TextDocument!, []);
    }
}
