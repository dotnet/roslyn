// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

/// <summary>
/// Request handler type exposed to typescript.
/// </summary>
internal abstract class AbstractVSTypeScriptRequestHandler<TRequestType, TResponseType> : ILspServiceRequestHandler<TRequestType, TResponseType>, IVSTypeScriptRequestHandler, ITextDocumentIdentifierHandler<TRequestType, TextDocumentIdentifier?>
{
    bool IMethodHandler.MutatesSolutionState => MutatesSolutionState;

    bool ISolutionRequiredHandler.RequiresLSPSolution => RequiresLSPSolution;

    public TextDocumentIdentifier? GetTextDocumentIdentifier(TRequestType request)
    {
        var typeScriptIdentifier = GetTypeSciptTextDocumentIdentifier(request);
        if (typeScriptIdentifier == null)
        {
            return null;
        }

        var textDocumentIdentifier = new VSTextDocumentIdentifier
        {
            Uri = typeScriptIdentifier.Value.Uri,
        };

        if (typeScriptIdentifier.Value.ProjectId != null)
        {
            textDocumentIdentifier.ProjectContext = new VSProjectContext
            {
                Id = typeScriptIdentifier.Value.ProjectId
            };
        }

        return textDocumentIdentifier;
    }

    public Task<TResponseType> HandleRequestAsync(TRequestType request, RequestContext context, CancellationToken cancellationToken)
    {
        return HandleRequestAsync(request, new TypeScriptRequestContext(context.Solution, context.Document), cancellationToken);
    }

    protected abstract bool MutatesSolutionState { get; }

    protected abstract bool RequiresLSPSolution { get; }

    protected abstract Task<TResponseType> HandleRequestAsync(TRequestType request, TypeScriptRequestContext context, CancellationToken cancellationToken);

    protected abstract TypeScriptTextDocumentIdentifier? GetTypeSciptTextDocumentIdentifier(TRequestType request);
}

internal record struct TypeScriptRequestContext(Solution? Solution, Document? Document);

/// <summary>
/// Custom type containing information in a <see cref="VSProjectContext"/> to avoid coupling LSP protocol versions.
/// </summary>
internal record struct TypeScriptTextDocumentIdentifier(Uri Uri, string? ProjectId);

internal interface IVSTypeScriptRequestHandler : ILspService
{
}
