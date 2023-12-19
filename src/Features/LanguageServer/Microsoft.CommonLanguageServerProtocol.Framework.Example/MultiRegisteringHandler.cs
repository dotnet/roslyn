// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Example;

public class MultiRegisteringHandler :
    IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>,
    IRequestHandler<DidChangeTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>,
    INotificationHandler<DidCloseTextDocumentParams, ExampleRequestContext>
{
    public bool MutatesSolutionState => throw new System.NotImplementedException();

    [LanguageServerEndpoint(Methods.TextDocumentDidCloseName)]
    Task INotificationHandler<DidCloseTextDocumentParams, ExampleRequestContext>.HandleNotificationAsync(DidCloseTextDocumentParams request, ExampleRequestContext requestContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    [LanguageServerEndpoint(Methods.TextDocumentDidOpenName)]
    Task<SemanticTokensDeltaPartialResult> IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>.HandleRequestAsync(DidOpenTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    [LanguageServerEndpoint(Methods.TextDocumentDidChangeName)]
    Task<SemanticTokensDeltaPartialResult> IRequestHandler<DidChangeTextDocumentParams, SemanticTokensDeltaPartialResult, ExampleRequestContext>.HandleRequestAsync(DidChangeTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
