// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentDiagnosticSourceProvider(string name) : IDiagnosticSourceProvider
{
    public bool IsDocument => true;
    public string Name => name;

    public abstract ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken);

    protected static TextDocument? GetOpenDocument(RequestContext context)
    {
        // Note: context.Document may be null in the case where the client is asking about a document that we have
        // since removed from the workspace.  In this case, we don't really have anything to process.
        // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
        //
        // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
        // handler treats those as separate worlds that they are responsible for.
        var textDocument = context.TextDocument;
        if (textDocument is null)
        {
            context.TraceInformation("Ignoring diagnostics request because no text document was provided");
            return null;
        }

        if (!context.IsTracking(textDocument.GetURI()))
        {
            context.TraceWarning($"Ignoring diagnostics request for untracked document: {textDocument.GetURI()}");
            return null;
        }

        return textDocument;
    }
}
