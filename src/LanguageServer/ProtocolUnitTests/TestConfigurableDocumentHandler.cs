// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Roslyn.Test.Utilities.AbstractLanguageServerProtocolTests;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal record TestRequestWithDocument([property: JsonPropertyName("textDocument"), JsonRequired] TextDocumentIdentifier TextDocumentIdentifier);

internal record TestConfigurableResponse([property: JsonPropertyName("response"), JsonRequired] string Response);

[ExportCSharpVisualBasicStatelessLspService(typeof(TestConfigurableDocumentHandler)), PartNotDiscoverable, Shared]
[LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TestConfigurableDocumentHandler() : ILspServiceDocumentRequestHandler<TestRequestWithDocument, TestConfigurableResponse>
{
    public const string MethodName = nameof(TestConfigurableDocumentHandler);

    private bool? _mutatesSolutionState;
    private bool? _requiresLSPSolution;
    private Task<TestConfigurableResponse>? _response;

    public bool MutatesSolutionState => _mutatesSolutionState ?? throw new InvalidOperationException($"{nameof(ConfigureHandler)} has not been called");
    public bool RequiresLSPSolution => _requiresLSPSolution ?? throw new InvalidOperationException($"{nameof(ConfigureHandler)} has not been called");

    public void ConfigureHandler(bool mutatesSolutionState, bool requiresLspSolution, Task<TestConfigurableResponse> response)
    {
        if (_mutatesSolutionState is not null || _requiresLSPSolution is not null || _response is not null)
        {
            throw new InvalidOperationException($"{nameof(ConfigureHandler)} has already been called");
        }

        _mutatesSolutionState = mutatesSolutionState;
        _requiresLSPSolution = requiresLspSolution;
        _response = response;
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestWithDocument request)
    {
        return request.TextDocumentIdentifier;
    }

    public Task<TestConfigurableResponse> HandleRequestAsync(TestRequestWithDocument request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_response, $"{nameof(ConfigureHandler)} has not been called");
        return _response;
    }

    public static void ConfigureHandler(TestLspServer server, bool mutatesSolutionState, bool requiresLspSolution, Task<TestConfigurableResponse> response)
    {
        var handler = (TestConfigurableDocumentHandler)server.GetQueueAccessor()!.Value.GetHandlerProvider().GetMethodHandler(TestConfigurableDocumentHandler.MethodName,
            TypeRef.From(typeof(TestRequestWithDocument)), TypeRef.From(typeof(TestConfigurableResponse)), LanguageServerConstants.DefaultLanguageName);
        handler.ConfigureHandler(mutatesSolutionState, requiresLspSolution, response);
    }
}
