// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorSpanMappingServiceWrapper : ISpanMappingService
    {
        private readonly IRazorSpanMappingService _razorSpanMappingService;

        public RazorSpanMappingServiceWrapper(IRazorSpanMappingService razorSpanMappingService)
        {
            _razorSpanMappingService = razorSpanMappingService ?? throw new ArgumentNullException(nameof(razorSpanMappingService));
        }

        public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            var razorSpans = await _razorSpanMappingService.MapSpansAsync(document, spans, cancellationToken).ConfigureAwait(false);
            var roslynSpans = new MappedSpanResult[razorSpans.Length];
            for (var i = 0; i < razorSpans.Length; i++)
            {
                var razorSpan = razorSpans[i];
                if (razorSpan.IsDefault)
                {
                    // Unmapped location
                    roslynSpans[i] = default;
                }
                else
                {
                    roslynSpans[i] = new MappedSpanResult(razorSpan.FilePath, razorSpan.LinePositionSpan, razorSpan.Span);
                }
            }

            return roslynSpans.ToImmutableArray();
        }
    }
}
