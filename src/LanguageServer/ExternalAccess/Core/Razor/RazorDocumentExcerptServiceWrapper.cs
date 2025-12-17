// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal sealed class RazorDocumentExcerptServiceWrapper : IDocumentExcerptService
{
    private readonly IRazorDocumentExcerptServiceImplementation _impl;

    public RazorDocumentExcerptServiceWrapper(IRazorDocumentExcerptServiceImplementation impl)
        => _impl = impl;

    public async Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, ClassificationOptions classificationOptions, CancellationToken cancellationToken)
    {
        var razorMode = mode switch
        {
            ExcerptMode.SingleLine => RazorExcerptMode.SingleLine,
            ExcerptMode.Tooltip => RazorExcerptMode.Tooltip,
            _ => throw ExceptionUtilities.UnexpectedValue(mode),
        };

        var result = await _impl.TryExcerptAsync(document, span, razorMode, new RazorClassificationOptionsWrapper(classificationOptions), cancellationToken).ConfigureAwait(false);

        if (result is null)
            return null;

        var razorExcerpt = result.Value;
        return new ExcerptResult(razorExcerpt.Content, razorExcerpt.MappedSpan, razorExcerpt.ClassifiedSpans, razorExcerpt.Document, razorExcerpt.Span);
    }
}
