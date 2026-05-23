// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteHtmlDocumentService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteHtmlDocumentService
{
    internal sealed class Factory : FactoryBase<IRemoteHtmlDocumentService>
    {
        protected override IRemoteHtmlDocumentService CreateService(in ServiceArgs args)
            => new RemoteHtmlDocumentService(in args);
    }

    public ValueTask<string?> GetHtmlDocumentTextAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetHtmlDocumentTextAsync(context, cancellationToken),
            cancellationToken);

    private async ValueTask<string?> GetHtmlDocumentTextAsync(RemoteDocumentContext documentContext, CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        return codeDocument.GetHtmlSourceText(cancellationToken).ToString();
    }
}
