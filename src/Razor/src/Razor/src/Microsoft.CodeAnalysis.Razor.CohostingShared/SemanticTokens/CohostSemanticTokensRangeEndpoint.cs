// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentSemanticTokensRangeName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostSemanticTokensRangeEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostSemanticTokensRangeEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    ITelemetryReporter telemetryReporter,
    ISemanticTokensLegendService semanticTokensLegendService)
    : CohostSemanticTokensEndpointBase<SemanticTokensRangeParams>(incompatibleProjectService, remoteServiceInvoker, telemetryReporter), IDynamicRegistrationProvider
{
    private readonly ISemanticTokensLegendService _semanticTokensLegendService = semanticTokensLegendService;

    protected override string LspMethodName => Methods.TextDocumentSemanticTokensRangeName;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.SemanticTokens?.DynamicRegistration == true)
        {
            var semanticTokensRefreshQueue = requestContext.GetRequiredService<IRazorSemanticTokensRefreshQueue>();
            semanticTokensRefreshQueue.Initialize(clientCapabilities);

            // We prefer Range over Full for performance reasons, so only advertise full support if Range isn't
            // available. The Range capability is SumType<bool, object> which is why the check is a bit odd.
            var supportsSemanticTokensRange = clientCapabilities.TextDocument?.SemanticTokens?.Requests?.Range?.Value is not (false or null);

            return [new Registration()
            {
                Method = Methods.TextDocumentSemanticTokensName,
                RegisterOptions = new SemanticTokensRegistrationOptions()
                    .EnableSemanticTokens(_semanticTokensLegendService, supportsSemanticTokensRange)
            }];
        }

        return [];
    }

    protected override Task<LinePositionSpan> GetRequestSpanAsync(SemanticTokensRangeParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => Task.FromResult(request.Range.ToLinePositionSpan());

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostSemanticTokensRangeEndpoint instance)
    {
        public Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, LinePositionSpan span, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(new SemanticTokensRangeParams { Range = span.ToRange() }, razorDocument, cancellationToken);
    }
}
