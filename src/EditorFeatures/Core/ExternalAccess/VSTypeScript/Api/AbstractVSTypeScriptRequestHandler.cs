// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.ExternalAccess.VSTypeScript.Api;

/// <summary>
/// Request handler type exposed to typescript 
/// </summary>
/// <typeparam name="TRequestType"></typeparam>
/// <typeparam name="TResponseType"></typeparam>
internal abstract class AbstractVSTypeScriptRequestHandler<TRequestType, TResponseType> : IRequestHandler<TRequestType, TResponseType>, IVSTypeScriptRequestHandler
{
    public abstract bool MutatesSolutionState { get; }

    public abstract bool RequiresLSPSolution { get; }

    public TextDocumentIdentifier? GetTextDocumentIdentifier(TRequestType request)
    {
        var typeScriptIdentifier = GetTypeSciptTextDocumentIdentifier(request);
        if (typeScriptIdentifier == null)
        {
            return null;
        }

        return new VSTextDocumentIdentifier
        {
            Uri = typeScriptIdentifier.Value.Uri,
            ProjectContext = new VSProjectContext
            {
                Id = typeScriptIdentifier.Value.ProjectId,
            }
        };
    }

    public Task<TResponseType> HandleRequestAsync(TRequestType request, RequestContext context, CancellationToken cancellationToken)
    {
        return HandleRequestAsync(request, new TypeScriptRequestContext(context.Solution, context.Document), cancellationToken);
    }

    protected abstract Task<TResponseType> HandleRequestAsync(TRequestType request, TypeScriptRequestContext context, CancellationToken cancellationToken);

    protected abstract TypeScriptTextDocumentIdentifier? GetTypeSciptTextDocumentIdentifier(TRequestType request);
}

internal record struct TypeScriptRequestContext(Solution? Solution, Document? Document);

/// <summary>
/// Custom type containing information in a <see cref="VSProjectContext"/> to avoid coupling LSP protocol versions.
/// </summary>
internal record struct TypeScriptTextDocumentIdentifier(Uri Uri, string ProjectId);

internal interface IVSTypeScriptRequestHandler
{
}
