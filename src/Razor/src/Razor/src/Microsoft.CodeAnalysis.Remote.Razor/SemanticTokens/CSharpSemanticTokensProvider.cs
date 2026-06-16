// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.SemanticTokens;

[Export(typeof(ICSharpSemanticTokensProvider)), Shared]
[method: ImportingConstructor]
internal sealed class CSharpSemanticTokensProvider(
    IClientCapabilitiesService clientCapabilitiesService,
    ITelemetryReporter telemetryReporter) : ICSharpSemanticTokensProvider
{
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public async Task<int[]?> GetCSharpSemanticTokensResponseAsync(RemoteDocumentContext documentContext, ImmutableArray<LinePositionSpan> csharpRanges, Guid correlationId, CancellationToken cancellationToken)
    {
        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentSemanticTokensRangeName,
            Constants.ExternalAccessServerName,
            TelemetryThresholds.SemanticTokensSubLSPTelemetryThreshold,
            correlationId);

        // We have a razor document, lets find the generated C# document
        var generatedDocument = await documentContext.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        var data = await SemanticTokensHelpers.HandleRequestHelperAsync(
            generatedDocument,
            csharpRanges,
            supportsVisualStudioExtensions: _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
            ClassificationOptions.Default,
            cancellationToken).ConfigureAwait(false);

        return data;
    }
}
