// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.LinkedEditingRange;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteLinkedEditingRangeService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteLinkedEditingRangeService
{
    internal sealed class Factory : FactoryBase<IRemoteLinkedEditingRangeService>
    {
        protected override IRemoteLinkedEditingRangeService CreateService(in ServiceArgs args)
            => new RemoteLinkedEditingRangeService(in args);
    }

    public ValueTask<LinePositionSpan[]?> GetRangesAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        LinePosition linePosition,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetRangesAsync(context, linePosition, cancellationToken),
            cancellationToken);

    public async ValueTask<LinePositionSpan[]?> GetRangesAsync(
        RemoteDocumentContext context,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        return LinkedEditingRangeHelper.GetLinkedSpans(linePosition, codeDocument);
    }
}
