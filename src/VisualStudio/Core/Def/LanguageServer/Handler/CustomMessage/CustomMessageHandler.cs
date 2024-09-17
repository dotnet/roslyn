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
        return request.Message.TextDocument!;
    }

    public async Task<CustomResponse> HandleRequestAsync(CustomMessageParams request, RequestContext context, CancellationToken cancellationToken)
    {
        return new CustomResponse(request.Message.Message, request.Message.Positions);

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
            var handlerMethod = handlerType.GetMethod("ExecuteAsync", BindingFlags.Instance);

            JsonSerializerOptions readOptions = new();
            LinePositionReadConverter linePositionReadConverter = new(request.Message.Positions.Select(tdp => ProtocolConversions.PositionToLinePosition(tdp)).ToArray());
            readOptions.Converters.Add(linePositionReadConverter);

            var messageType = handlerMethod.GetParameters()[0].ParameterType;
            var deserializedMessage = JsonSerializer.Deserialize(request.Message.Message, messageType, readOptions);

            var handler = Activator.CreateInstance(handlerType);
            var parameters = new object?[] { deserializedMessage, context.Document, cancellationToken };
            var resultTask = (Task)handlerMethod.Invoke(handler, parameters);

            await resultTask.ConfigureAwait(false);

            var resultProperty = resultTask.GetType().GetProperty("Result");
            var result = resultProperty.GetValue(resultTask);

            JsonSerializerOptions writeOptions = new();
            LinePositionWriteConverter linePositionWriteConverter = new();
            writeOptions.Converters.Add(linePositionWriteConverter);

            var resultJson = JsonSerializer.Serialize(result, resultProperty.PropertyType, writeOptions);

            return new CustomResponse(
                JsonNode.Parse(resultJson)!,
                linePositionWriteConverter.LinePositions
                    .OrderBy(p => p.Value)
                    .Select(p => new Position(p.Key.Line, p.Key.Character))
                    .ToArray());
        }
        finally
        {
            AppDomain.Unload(appDomain);
        }
#pragma warning restore CS0162 // Unreachable code detected
    }
}
