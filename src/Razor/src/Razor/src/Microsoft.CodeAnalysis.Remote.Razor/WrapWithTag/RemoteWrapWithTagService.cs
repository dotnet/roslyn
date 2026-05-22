// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Text.LinePositionSpan>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed partial class RemoteWrapWithTagService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteWrapWithTagService
{
    internal sealed class Factory : FactoryBase<IRemoteWrapWithTagService>
    {
        protected override IRemoteWrapWithTagService CreateService(in ServiceArgs args)
            => new RemoteWrapWithTagService(in args);
    }

    public ValueTask<Response> GetValidWrappingRangeAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        LinePositionSpan range,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetValidWrappingRangeAsync(context, range, cancellationToken),
            cancellationToken);

    private static async ValueTask<Response> GetValidWrappingRangeAsync(
        RemoteDocumentContext context,
        LinePositionSpan range,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (WrapWithTagHelper.TryGetValidWrappingRange(codeDocument, range, out var adjustedRange))
        {
            return Response.Results(adjustedRange);
        }

        return Response.NoFurtherHandling;
    }
}
