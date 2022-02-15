// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentExcerptServiceWrapper : IDocumentExcerptService
    {
        [Obsolete]
        private readonly IRazorDocumentExcerptService? _legacyRazorDocumentExcerptService;

        private readonly IRazorDocumentExcerptServiceImplementation? _impl;

        [Obsolete]
        public RazorDocumentExcerptServiceWrapper(IRazorDocumentExcerptService razorDocumentExcerptService)
            => _legacyRazorDocumentExcerptService = razorDocumentExcerptService;

        public RazorDocumentExcerptServiceWrapper(IRazorDocumentExcerptServiceImplementation impl)
            => _impl = impl;

        public async Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, CancellationToken cancellationToken)
        {
            var razorMode = mode switch
            {
                ExcerptMode.SingleLine => RazorExcerptMode.SingleLine,
                ExcerptMode.Tooltip => RazorExcerptMode.Tooltip,
                _ => throw ExceptionUtilities.UnexpectedValue(mode),
            };

            RazorExcerptResult? result;
            if (_impl != null)
            {
                var options = ClassificationOptions.From(document.Project);
                result = await _impl.TryExcerptAsync(document, span, razorMode, new RazorClassificationOptionsWrapper(options), cancellationToken).ConfigureAwait(false);
            }
            else
            {
#pragma warning disable CS0612 // Type or member is obsolete
                Contract.ThrowIfNull(_legacyRazorDocumentExcerptService);
                result = await _legacyRazorDocumentExcerptService.TryExcerptAsync(document, span, razorMode, cancellationToken).ConfigureAwait(false);
#pragma warning restore
            }

            var razorExcerpt = result.Value;
            return new ExcerptResult(razorExcerpt.Content, razorExcerpt.MappedSpan, razorExcerpt.ClassifiedSpans, razorExcerpt.Document, razorExcerpt.Span);
        }
    }
}
