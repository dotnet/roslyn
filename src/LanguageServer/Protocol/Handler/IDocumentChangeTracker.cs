// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Associates LSP document URIs with the roslyn source text containing the LSP document text.
/// Called via <see cref="DidOpenHandler"/>, <see cref="DidChangeHandler"/> and <see cref="DidCloseHandler"/>
/// </summary>
internal interface IDocumentChangeTracker
{
    ValueTask StartTrackingAsync(DocumentUri documentUri, SourceText initialText, string languageId, int lspVersion, CancellationToken cancellationToken);
    void UpdateTrackedDocument(DocumentUri documentUri, SourceText text, int lspVersion);
    ValueTask StopTrackingAsync(DocumentUri documentUri, CancellationToken cancellationToken);
}

internal sealed class NonMutatingDocumentChangeTracker : IDocumentChangeTracker
{
    public ValueTask StartTrackingAsync(DocumentUri documentUri, SourceText initialText, string languageId, int lspVersion, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
    }

    public ValueTask StopTrackingAsync(DocumentUri documentUri, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
    }

    public void UpdateTrackedDocument(DocumentUri documentUri, SourceText text, int lspVersion)
    {
        throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
    }
}
