// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

/// <summary>
/// Request handler type exposed to typescript.
/// </summary>
internal abstract class AbstractEditorConfigRequestHandler<TRequestType, TResponseType> : IRequestHandler<TRequestType, TResponseType>, IVSTypeScriptRequestHandler
{
    bool IRequestHandler.MutatesSolutionState => MutatesSolutionState;

    bool IRequestHandler.RequiresLSPSolution => RequiresLSPSolution;

    TextDocumentIdentifier? IRequestHandler<TRequestType, TResponseType>.GetTextDocumentIdentifier(TRequestType request)
    {
        var editorConfigIdentifier = GetEditorConfigTextDocumentIdentifier(request);
        if (editorConfigIdentifier == null)
        {
            return null;
        }

        var textDocumentIdentifier = new VSTextDocumentIdentifier
        {
            Uri = editorConfigIdentifier.Value.Uri,
        };

        if (editorConfigIdentifier.Value.ProjectId != null)
        {
            textDocumentIdentifier.ProjectContext = new VSProjectContext
            {
                Id = editorConfigIdentifier.Value.ProjectId
            };
        }

        return textDocumentIdentifier;
    }

    Task<TResponseType> IRequestHandler<TRequestType, TResponseType>.HandleRequestAsync(TRequestType request, RequestContext context, CancellationToken cancellationToken)
    {
        return HandleRequestAsync(request, new EditorConfigRequestContext(context.Solution, context.Document), cancellationToken);
    }

    protected abstract bool MutatesSolutionState { get; }

    protected abstract bool RequiresLSPSolution { get; }

    protected abstract Task<TResponseType> HandleRequestAsync(TRequestType request, EditorConfigRequestContext context, CancellationToken cancellationToken);

    protected abstract EditorConfigTextDocumentIdentifier? GetEditorConfigTextDocumentIdentifier(TRequestType request);
}

internal record struct EditorConfigRequestContext(Solution? Solution, Document? Document);

/// <summary>
/// Custom type containing information in a <see cref="VSProjectContext"/> to avoid coupling LSP protocol versions.
/// </summary>
internal record struct EditorConfigTextDocumentIdentifier(Uri Uri, string? ProjectId);

internal interface IEditorConfigRequestHandler : ILspService
{
}
