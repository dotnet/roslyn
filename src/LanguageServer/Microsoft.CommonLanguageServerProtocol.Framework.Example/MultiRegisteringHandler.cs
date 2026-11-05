// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Example;

internal sealed class MultiRegisteringHandler :
    IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDelta, ExampleRequestContext>,
    IRequestHandler<DidChangeTextDocumentParams, SemanticTokensDelta, ExampleRequestContext>,
    INotificationHandler<DidCloseTextDocumentParams, ExampleRequestContext>
{
    public bool MutatesSolutionState => throw new System.NotImplementedException();

    [LanguageServerEndpoint(Methods.TextDocumentDidCloseName, LanguageServerConstants.DefaultLanguageName)]
    Task INotificationHandler<DidCloseTextDocumentParams, ExampleRequestContext>.HandleNotificationAsync(DidCloseTextDocumentParams request, ExampleRequestContext requestContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    [LanguageServerEndpoint(Methods.TextDocumentDidOpenName, LanguageServerConstants.DefaultLanguageName)]
    Task<SemanticTokensDelta> IRequestHandler<DidOpenTextDocumentParams, SemanticTokensDelta, ExampleRequestContext>.HandleRequestAsync(DidOpenTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    [LanguageServerEndpoint(Methods.TextDocumentDidChangeName, LanguageServerConstants.DefaultLanguageName)]
    Task<SemanticTokensDelta> IRequestHandler<DidChangeTextDocumentParams, SemanticTokensDelta, ExampleRequestContext>.HandleRequestAsync(DidChangeTextDocumentParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
