// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Example;

public class MultiRegisteringHandler :
    IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>,
    IRequestHandler<DidChangeTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>,
    INotificationHandler<DidCloseTextDocumentParams, ExampleRequestContext>
{
    public bool MutatesSolutionState => throw new System.NotImplementedException();

    [LanguageServerEndpoint(Methods.TextDocumentDidCloseName)]
    public Task HandleNotificationAsync(DidCloseTextDocumentParams request, ExampleRequestContext requestContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    [LanguageServerEndpoint(Methods.TextDocumentDidOpenName)]
    public Task<SemanticTokensDeltaPartialResult> HandleRequestAsync(DidOpenTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    [LanguageServerEndpoint(Methods.TextDocumentDidChangeName)]
    public Task<SemanticTokensDeltaPartialResult> HandleRequestAsync(DidChangeTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
