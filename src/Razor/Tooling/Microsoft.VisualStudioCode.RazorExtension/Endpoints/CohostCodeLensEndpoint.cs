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
[CohostEndpoint(Methods.TextDocumentCodeLensName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostCodeLensEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostCodeLensEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<CodeLensParams, LspCodeLens[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;
    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.CodeLens?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentCodeLensName,
                RegisterOptions = new CodeLensRegistrationOptions()
                {
                    ResolveProvider = true
                }
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(CodeLensParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<LspCodeLens[]?> HandleRequestAsync(CodeLensParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeLensService, LspCodeLens[]?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetCodeLensAsync(solutionInfo, razorDocument.Id, request.TextDocument, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostCodeLensEndpoint instance)
    {
        public Task<LspCodeLens[]?> HandleRequestAsync(CodeLensParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
