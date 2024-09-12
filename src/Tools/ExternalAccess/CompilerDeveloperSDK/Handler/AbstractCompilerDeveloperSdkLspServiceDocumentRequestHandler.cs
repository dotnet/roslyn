// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal abstract class AbstractCompilerDeveloperSdkLspServiceDocumentRequestHandler<TRequest, TResponse> : ILspServiceDocumentRequestHandler<TRequest, TResponse>
{
    public abstract bool RequiresLSPSolution { get; }
    public abstract Task<TResponse> HandleRequestAsync(TRequest request, RequestContext context, CancellationToken cancellationToken);
    public abstract Uri GetTextDocumentIdentifier(TRequest request);
    public abstract bool MutatesSolutionState { get; }

    bool IMethodHandler.MutatesSolutionState => MutatesSolutionState;
    bool ISolutionRequiredHandler.RequiresLSPSolution => RequiresLSPSolution;

    TextDocumentIdentifier ITextDocumentIdentifierHandler<TRequest, TextDocumentIdentifier>.GetTextDocumentIdentifier(TRequest request)
        => new() { Uri = GetTextDocumentIdentifier(request) };
    Task<TResponse> IRequestHandler<TRequest, TResponse, LspRequestContext>.HandleRequestAsync(TRequest request, LspRequestContext context, CancellationToken cancellationToken)
        => HandleRequestAsync(request, new RequestContext(context), cancellationToken);
}
