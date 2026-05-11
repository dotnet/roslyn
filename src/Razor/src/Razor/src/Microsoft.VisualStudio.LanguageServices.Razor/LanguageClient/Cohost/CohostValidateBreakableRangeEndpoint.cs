// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(VSInternalMethods.TextDocumentValidateBreakableRangeName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportCohostStatelessLspService(typeof(CohostValidateBreakableRangeEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostValidateBreakableRangeEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<VSInternalValidateBreakableRangeParams, LspRange?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        return [new Registration
        {
            Method = VSInternalMethods.TextDocumentValidateBreakableRangeName,
            RegisterOptions = new TextDocumentRegistrationOptions()
        }];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSInternalValidateBreakableRangeParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override Task<LspRange?> HandleRequestAsync(VSInternalValidateBreakableRangeParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(razorDocument, request.Range.ToLinePositionSpan(), cancellationToken);

    private async Task<LspRange?> HandleRequestAsync(TextDocument razorDocument, LinePositionSpan span, CancellationToken cancellationToken)
    {
        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteDebugInfoService, LinePositionSpan?>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.ValidateBreakableRangeAsync(solutionInfo, razorDocument.Id, span, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        return response?.ToRange();
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostValidateBreakableRangeEndpoint instance)
    {
        public Task<LspRange?> HandleRequestAsync(TextDocument razorDocument, LinePositionSpan span, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, span, cancellationToken);
    }
}
