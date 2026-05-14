// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
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
    : AbstractCohostDocumentEndpoint<SemanticTokensRangeParams, SemanticTokens?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    protected override bool MutatesSolutionState => false;
    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(SemanticTokensRangeParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<SemanticTokens?> HandleRequestAsync(SemanticTokensRangeParams request, RazorCohostRequestContext context, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await HandleRequestAsync(request, razorDocument, cancellationToken).ConfigureAwait(false);

        if (result is not null)
        {
            // Roslyn uses frozen semantics for semantic tokens, so it could return results from an older project state.
            // Every time they get a request they queue up a refresh, which will check the project checksums, and if there
            // hasn't been any changes, will no-op. We call into that same logic here to ensure everything is up to date.
            // See: https://github.com/dotnet/roslyn/blob/bb57f4643bb3d52eb7626f9863da177d9e219f1e/src/LanguageServer/Protocol/Handler/SemanticTokens/SemanticTokensHelpers.cs#L48-L52
            var semanticTokensWrapperService = context.GetRequiredService<IRazorSemanticTokensRefreshQueue>();
            await semanticTokensWrapperService.TryEnqueueRefreshComputationAsync(razorDocument.Project, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    protected override Task<SemanticTokens?> HandleRequestAsync(SemanticTokensRangeParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(razorDocument, request.Range.ToLinePositionSpan(), cancellationToken);

    private async Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, LinePositionSpan span, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentSemanticTokensRangeName, RazorLSPConstants.CohostLanguageServerName, TelemetryThresholds.SemanticTokensRazorTelemetryThreshold, correlationId);

        var tokens = await _remoteServiceInvoker.TryInvokeAsync<IRemoteSemanticTokensService, int[]?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetSemanticTokensDataAsync(solutionInfo, razorDocument.Id, span, correlationId, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (tokens is not null)
        {
            return new SemanticTokens
            {
                Data = tokens
            };
        }

        return null;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostSemanticTokensRangeEndpoint instance)
    {
        public Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, LinePositionSpan span, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, span, cancellationToken);
    }
}
