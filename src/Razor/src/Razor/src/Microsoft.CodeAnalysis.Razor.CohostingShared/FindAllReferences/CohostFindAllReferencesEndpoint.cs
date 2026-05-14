// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentReferencesName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostFindAllReferencesEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostFindAllReferencesEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<ReferenceParams, SumType<VSInternalReferenceItem, LspLocation>[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.References?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.TextDocumentReferencesName,
                RegisterOptions = new ReferenceRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(ReferenceParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override Task<SumType<VSInternalReferenceItem, LspLocation>[]?> HandleRequestAsync(ReferenceParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(razorDocument, request.Position, cancellationToken);

    private async Task<SumType<VSInternalReferenceItem, LspLocation>[]?> HandleRequestAsync(TextDocument razorDocument, Position position, CancellationToken cancellationToken)
    {
        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteFindAllReferencesService, RemoteResponse<SumType<VSInternalReferenceItem, LspLocation>[]?>>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.FindAllReferencesAsync(solutionInfo, razorDocument.Id, position, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Result is SumType<VSInternalReferenceItem, LspLocation>[] results)
        {
            return results;
        }

        return null;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostFindAllReferencesEndpoint instance)
    {
        public Task<SumType<VSInternalReferenceItem, LspLocation>[]?> HandleRequestAsync(TextDocument razorDocument, Position position, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, position, cancellationToken);
    }
}
