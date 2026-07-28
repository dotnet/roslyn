// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentSemanticTokensFullName)]
[ExportRazorStatelessLspService(typeof(CohostSemanticTokensFullEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostSemanticTokensFullEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    ITelemetryReporter telemetryReporter)
    : CohostSemanticTokensEndpointBase<SemanticTokensFullParams>(incompatibleProjectService, remoteServiceInvoker, telemetryReporter)
{
    protected override string LspMethodName => Methods.TextDocumentSemanticTokensFullName;

    protected override async Task<LinePositionSpan> GetRequestSpanAsync(SemanticTokensFullParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var sourceText = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Full semantic tokens requests always cover the entire document.
        return new LinePositionSpan(new(0, 0), new(sourceText.Lines.Count, 0));
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostSemanticTokensFullEndpoint instance)
    {
        public Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request: default!, razorDocument, cancellationToken);
    }
}
