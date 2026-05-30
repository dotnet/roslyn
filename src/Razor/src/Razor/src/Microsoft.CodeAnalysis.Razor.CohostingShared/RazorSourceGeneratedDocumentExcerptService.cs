// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.DocumentExcerpt;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CohostingShared;

#pragma warning disable RS0030 // Do not use banned APIs
[Export(typeof(IRazorSourceGeneratedDocumentExcerptService))]
[Shared]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class RazorSourceGeneratedDocumentExcerptService(IRemoteServiceInvoker remoteServiceInvoker) : IRazorSourceGeneratedDocumentExcerptService
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    public async Task<ExcerptResult?> TryExcerptAsync(SourceGeneratedDocument document, TextSpan span, ExcerptMode mode, ClassificationOptions options, CancellationToken cancellationToken)
    {
        if (!document.IsRazorSourceGeneratedDocument())
        {
            return null;
        }

        var result = await _remoteServiceInvoker.TryInvokeAsync<IRemoteSpanMappingService, RemoteExcerptResult?>(
            document.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.TryExcerptAsync(solutionInfo, document.Id, span, mode, options, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        // Source text can't be sent back from OOP, so we have to do the translation here. Fortunately this doesn't need
        // anything we can't access
        var razorDocument = document.Project.GetAdditionalDocument(result.RazorDocumentId);
        if (razorDocument is null)
        {
            return null;
        }

        var razorSourceText = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var builder = result.ClassifiedSpans.ToBuilder();

        var razorDocumentSpan = result.RazorDocumentSpan;
        var excerptSpan = result.ExcerptSpan;
        var excerptText = RazorDocumentExcerptHelper.GetTranslatedExcerptText(razorSourceText, ref razorDocumentSpan, ref excerptSpan, builder);

        return new ExcerptResult(excerptText, razorDocumentSpan, builder.ToImmutable(), document, span);
    }
}
