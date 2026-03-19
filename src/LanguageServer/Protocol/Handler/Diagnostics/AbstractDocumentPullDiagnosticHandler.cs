// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
    IDiagnosticsRefresher diagnosticRefresher,
    IDiagnosticSourceManager diagnosticSourceManager,
    IGlobalOptionService globalOptions)
    : AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
        diagnosticRefresher,
        globalOptions), ITextDocumentIdentifierHandler<TDiagnosticsParams, TextDocumentIdentifier?>
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    protected readonly IDiagnosticSourceManager DiagnosticSourceManager = diagnosticSourceManager;

    public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);

    protected override async ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(TDiagnosticsParams diagnosticsParams, string? requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken)
    {
        // Note: context.Document may be null in the case where the client is asking about a document that we have
        // since removed from the workspace.  In this case, we don't really have anything to process.
        // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
        //
        // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
        // handler treats those as separate worlds that they are responsible for.
        var identifier = GetTextDocumentIdentifier(diagnosticsParams);
        if (identifier is null || context.TextDocument is null)
        {
            context.TraceDebug("Ignoring diagnostics request because no text document was provided");
            return [];
        }

        if (!context.IsTracking(identifier.DocumentUri))
        {
            context.TraceWarning($"Ignoring diagnostics request for untracked document: {identifier.DocumentUri}");
            return [];
        }

        return await DiagnosticSourceManager.CreateDocumentDiagnosticSourcesAsync(context, requestDiagnosticCategory, cancellationToken).ConfigureAwait(false);
    }
}
