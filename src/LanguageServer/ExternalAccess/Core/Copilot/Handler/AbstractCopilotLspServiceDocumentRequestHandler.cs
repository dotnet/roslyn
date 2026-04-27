// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Copilot;

/// <inheritdoc cref="ILspServiceDocumentRequestHandler{TRequest, TResponse}"/>
internal abstract class AbstractCopilotLspServiceDocumentRequestHandler<TRequest, TResponse> : ILspServiceDocumentRequestHandler<TRequest, TResponse>
{
    public abstract Task<TResponse> HandleRequestAsync(TRequest request, CopilotRequestContext context, CancellationToken cancellationToken);
    public abstract Uri GetTextDocumentUri(TRequest request);

    bool IMethodHandler.MutatesSolutionState => false;
    bool ISolutionRequiredHandler.RequiresLSPSolution => true;

    TextDocumentIdentifier ITextDocumentIdentifierHandler<TRequest, TextDocumentIdentifier>.GetTextDocumentIdentifier(TRequest request)
        => new() { DocumentUri = new(GetTextDocumentUri(request)) };

    Task<TResponse> IRequestHandler<TRequest, TResponse, RequestContext>.HandleRequestAsync(TRequest request, RequestContext context, CancellationToken cancellationToken)
        => HandleRequestAsync(request, new CopilotRequestContext(context), cancellationToken);
}
