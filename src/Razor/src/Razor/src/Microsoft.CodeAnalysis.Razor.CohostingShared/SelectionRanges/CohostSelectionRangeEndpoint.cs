// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentSelectionRangeName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostSelectionRangeEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostSelectionRangeEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<SelectionRangeParams, SelectionRange[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.SelectionRange?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.TextDocumentSelectionRangeName,
                RegisterOptions = new SelectionRangeRegistrationOptions()
            }];
        }

        return [];
    }

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(SelectionRangeParams request)
        => request.TextDocument;

    protected override Task<SelectionRange[]?> HandleRequestAsync(SelectionRangeParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(razorDocument, request.Positions, cancellationToken);

    private Task<SelectionRange[]?> HandleRequestAsync(TextDocument razorDocument, Position[] positions, CancellationToken cancellationToken)
        => _remoteServiceInvoker.TryInvokeAsync<IRemoteSelectionRangeService, SelectionRange[]?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetSelectionRangesAsync(solutionInfo, razorDocument.Id, positions, cancellationToken),
            cancellationToken).AsTask();

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostSelectionRangeEndpoint instance)
    {
        public Task<SelectionRange[]?> HandleRequestAsync(TextDocument razorDocument, Position[] positions, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, positions, cancellationToken);
    }
}
