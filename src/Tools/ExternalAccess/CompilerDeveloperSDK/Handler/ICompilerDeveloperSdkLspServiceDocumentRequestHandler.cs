// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal interface ICompilerDeveloperSdkLspServiceDocumentRequestHandler<TRequest, TResponse> :
    ILspServiceDocumentRequestHandler<TRequest, TResponse>
{
    bool ISolutionRequiredHandler.RequiresLSPSolution => true;

    Task<TResponse> IRequestHandler<TRequest, TResponse, LspRequestContext>.HandleRequestAsync(TRequest request, LspRequestContext context, CancellationToken cancellationToken)
        => HandleRequestAsync(request, new RequestContext(context), cancellationToken);

    Task<TResponse> HandleRequestAsync(TRequest request, RequestContext context, CancellationToken cancellationToken);
}
