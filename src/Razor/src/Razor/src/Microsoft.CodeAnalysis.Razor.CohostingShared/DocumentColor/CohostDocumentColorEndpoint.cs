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

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentDocumentColorName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostDocumentColorEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostDocumentColorEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<DocumentColorParams, ColorInformation[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        return [new Registration
        {
            Method = Methods.TextDocumentDocumentColorName,
            RegisterOptions = new DocumentColorRegistrationOptions()
        }];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(DocumentColorParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<ColorInformation[]?> HandleRequestAsync(DocumentColorParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        return await _requestInvoker.MakeHtmlLspRequestAsync<DocumentColorParams, ColorInformation[]>
        (
            razorDocument,
            Methods.TextDocumentDocumentColorName,
            request,
            cancellationToken).ConfigureAwait(false);
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostDocumentColorEndpoint instance)
    {
        public Task<ColorInformation[]?> HandleRequestAsync(DocumentColorParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
