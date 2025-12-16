
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[ExportWorkspaceService(typeof(ISourceGeneratedDocumentExcerptService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorSourceGeneratedDocumentExcerptServiceWrapper(
    [Import(AllowDefault = true)] IRazorSourceGeneratedDocumentExcerptService? implementation) : ISourceGeneratedDocumentExcerptService
{
    private readonly IRazorSourceGeneratedDocumentExcerptService? _implementation = implementation;

    public bool CanExcerpt(SourceGeneratedDocument document)
    {
        return _implementation is not null && document.IsRazorSourceGeneratedDocument();
    }

    public async Task<ExcerptResult?> TryExcerptAsync(SourceGeneratedDocument document, TextSpan span, ExcerptMode mode, ClassificationOptions classificationOptions, CancellationToken cancellationToken)
    {
        if (_implementation is null || !document.IsRazorSourceGeneratedDocument())
        {
            return null;
        }

        var razorMode = mode switch
        {
            ExcerptMode.SingleLine => RazorExcerptMode.SingleLine,
            ExcerptMode.Tooltip => RazorExcerptMode.Tooltip,
            _ => throw ExceptionUtilities.UnexpectedValue(mode),
        };

        var options = new RazorClassificationOptionsWrapper(classificationOptions);
        var result = await _implementation.TryExcerptAsync(document, span, razorMode, options, cancellationToken).ConfigureAwait(false);

        if (result is null)
            return null;

        var razorExcerpt = result.Value;
        return new ExcerptResult(razorExcerpt.Content, razorExcerpt.MappedSpan, razorExcerpt.ClassifiedSpans, razorExcerpt.Document, razorExcerpt.Span);
    }
}
