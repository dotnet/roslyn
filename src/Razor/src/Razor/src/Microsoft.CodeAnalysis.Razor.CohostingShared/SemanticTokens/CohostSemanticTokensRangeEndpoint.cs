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
[CohostEndpoint(Methods.TextDocumentSemanticTokensRangeName)]
[ExportRazorStatelessLspService(typeof(CohostSemanticTokensRangeEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostSemanticTokensRangeEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    ITelemetryReporter telemetryReporter)
    : CohostSemanticTokensEndpointBase<SemanticTokensRangeParams>(incompatibleProjectService, remoteServiceInvoker, telemetryReporter)
{
    protected override string LspMethodName => Methods.TextDocumentSemanticTokensRangeName;

    protected override Task<LinePositionSpan> GetRequestSpanAsync(SemanticTokensRangeParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => Task.FromResult(request.Range.ToLinePositionSpan());

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostSemanticTokensRangeEndpoint instance)
    {
        public Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, LinePositionSpan span, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(new SemanticTokensRangeParams { Range = span.ToRange() }, razorDocument, cancellationToken);
    }
}
