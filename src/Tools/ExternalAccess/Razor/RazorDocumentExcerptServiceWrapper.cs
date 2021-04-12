// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentExcerptServiceWrapper : IDocumentExcerptService
    {
        private readonly IRazorDocumentExcerptService _razorDocumentExcerptService;

        public RazorDocumentExcerptServiceWrapper(IRazorDocumentExcerptService razorDocumentExcerptService)
        {
            _razorDocumentExcerptService = razorDocumentExcerptService ?? throw new ArgumentNullException(nameof(razorDocumentExcerptService));
        }

        public async Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, CancellationToken cancellationToken)
        {
            var razorMode = mode switch
            {
                ExcerptMode.SingleLine => RazorExcerptMode.SingleLine,
                ExcerptMode.Tooltip => RazorExcerptMode.Tooltip,
                _ => throw new InvalidEnumArgumentException($"Unsupported enum type {mode}."),
            };
            var nullableRazorExcerpt = await _razorDocumentExcerptService.TryExcerptAsync(document, span, razorMode, cancellationToken).ConfigureAwait(false);
            if (nullableRazorExcerpt == null)
            {
                return null;
            }

            var razorExcerpt = nullableRazorExcerpt.Value;
            var roslynExcerpt = new ExcerptResult(razorExcerpt.Content, razorExcerpt.MappedSpan, razorExcerpt.ClassifiedSpans, razorExcerpt.Document, razorExcerpt.Span);
            return roslynExcerpt;
        }
    }
}
