// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Handler for a workspace request with parameters <typeparamref name="TRequest"/> and result <typeparamref name="TResponse"/>.
/// </summary>
internal interface ILspServiceRequestHandler<TRequest, TResponse> :
    ILspService,
    IRequestHandler<TRequest, TResponse, RequestContext>,
    ISolutionRequiredHandler
{
}

/// <summary>
/// Handler for a workspace parameter-less request with result <typeparamref name="TResponse"/>.
/// </summary>
internal interface ILspServiceRequestHandler<TResponse> :
    ILspService,
    IRequestHandler<TResponse, RequestContext>,
    ISolutionRequiredHandler
{
}

/// <summary>
/// Handler for document request with parameters <typeparamref name="TRequest"/> and result <typeparamref name="TResponse"/>.
/// </summary>
internal interface ILspServiceDocumentRequestHandler<TRequest, TResponse> :
    ILspServiceRequestHandler<TRequest, TResponse>,
    ITextDocumentIdentifierHandler<TRequest, TextDocumentIdentifier>,
    ISolutionRequiredHandler
{
}
