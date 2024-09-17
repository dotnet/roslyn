// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

[ExportCSharpVisualBasicStatelessLspService(typeof(CustomMessageHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CustomMessageHandler()
    : ILspServiceDocumentRequestHandler<CustomMessageParams, CustomMessage>
{
    private const string MethodName = "roslyn/customMessage";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(CustomMessageParams request)
    {
        return request.Message.TextDocumentPositions.First().TextDocument;
    }

    public async Task<CustomMessage> HandleRequestAsync(CustomMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        return request.Message;

#pragma warning disable CS0162 // Unreachable code detected
        AppDomain? appDomain = null;
        try
        {
            var basePath = Path.GetDirectoryName(request.AssemblyPath);
            appDomain = AppDomain.CreateDomain(request.TypeFullName, null, new AppDomainSetup()
            {
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationBase = basePath,
            });

            appDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
            {
                Assembly? assembly = null;
                try
                {
                    assembly = Assembly.Load(args.Name);
                    if (assembly != null)
                        return assembly;
                }
                catch { }

                var name = new AssemblyName(args.Name);
                var possiblePath = Path.Combine(basePath, $"{name.Name}.dll");
                assembly = Assembly.LoadFrom(possiblePath);

                if (assembly != null)
                    return assembly;

                return null;
            };

            var assemblyName = AssemblyName.GetAssemblyName(request.AssemblyPath);
            var assembly = appDomain.Load(assemblyName);

            var handlerType = assembly.GetType(request.TypeFullName);
            var handlerMethod = handlerType.GetMethod("ExecuteAsync", BindingFlags.Static);

            var messageType = handlerMethod.GetParameters()[0].ParameterType;
            var deserializedMessage = JsonSerializer.Deserialize(request.Message.Message, messageType);

            var linePositions = request.Message.TextDocumentPositions.Select(tdp => ProtocolConversions.PositionToLinePosition(tdp.Position)).ToArray();

            var parameters = new object?[] { deserializedMessage, context.Document, linePositions, cancellationToken };
            var resultTask = (Task)handlerMethod.Invoke(null, parameters);

            await resultTask.ConfigureAwait(false);

            var resultProperty = resultTask.GetType().GetProperty("Result");
            var result = resultProperty.GetValue(resultTask);

            var resultJson = JsonSerializer.Serialize(result, resultProperty.PropertyType);

            return new CustomMessage(JsonNode.Parse(resultJson)!, []);
        }
        finally
        {
            AppDomain.Unload(appDomain);
        }
#pragma warning restore CS0162 // Unreachable code detected
    }
}
