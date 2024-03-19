// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorSpanMappingServiceWrapper : AbstractSpanMappingService
    {
        private readonly IRazorSpanMappingService _razorSpanMappingService;

        public RazorSpanMappingServiceWrapper(IRazorSpanMappingService razorSpanMappingService)
        {
            _razorSpanMappingService = razorSpanMappingService ?? throw new ArgumentNullException(nameof(razorSpanMappingService));
        }

        /// <summary>
        /// Modern razor span mapping service can handle if we add imports.  Razor will then rewrite that
        /// to their own form.
        /// </summary>
        public override bool SupportsMappingImportDirectives => true;

        public override async Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
            Document oldDocument,
            Document newDocument,
            CancellationToken cancellationToken)
        {
            var diffService = newDocument.Project.Solution.Services.GetRequiredService<IDocumentTextDifferencingService>();

            // This is a hack that finds a minimal diff. It's not the ideal algorithm but should cover most scenarios. In the future,
            // we should improve this algorithm - see https://github.com/dotnet/roslyn/issues/53346 for additional details.
            var textChanges = await diffService.GetTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
            var mappedSpanResults = await MapSpansAsync(oldDocument, textChanges.Select(tc => tc.Span), cancellationToken).ConfigureAwait(false);

            var mappedTextChanges = MatchMappedSpansToTextChanges(textChanges, mappedSpanResults);
            return mappedTextChanges;
        }

        public override async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(
            Document document,
            IEnumerable<TextSpan> spans,
            CancellationToken cancellationToken)
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
