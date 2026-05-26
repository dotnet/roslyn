// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.SemanticTokens;

[Export(typeof(ICSharpSemanticTokensProvider)), Shared]
[method: ImportingConstructor]
internal class RemoteCSharpSemanticTokensProvider(
    IClientCapabilitiesService clientCapabilitiesService,
    ITelemetryReporter telemetryReporter) : ICSharpSemanticTokensProvider
{
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public async Task<int[]?> GetCSharpSemanticTokensResponseAsync(DocumentContext documentContext, ImmutableArray<LinePositionSpan> csharpRanges, Guid correlationId, CancellationToken cancellationToken)
    {
        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentSemanticTokensRangeName,
            Constants.ExternalAccessServerName,
            TelemetryThresholds.SemanticTokensSubLSPTelemetryThreshold,
            correlationId);

        // We have a razor document, lets find the generated C# document
        Debug.Assert(documentContext is RemoteDocumentContext, "This method only works on document snapshots created in the OOP process");
        var snapshot = (RemoteDocumentSnapshot)documentContext.Snapshot;
        var generatedDocument = await snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var data = await SemanticTokensRange
            .GetSemanticTokensAsync(
                generatedDocument,
                csharpRanges,
                supportsVisualStudioExtensions: _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
                cancellationToken)
            .ConfigureAwait(false);

        return data;
    }
}
