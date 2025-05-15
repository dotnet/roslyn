// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal sealed class RazorMappingServiceWrapper(IRazorMappingService razorMappingService) : ISpanMappingService
{
    private readonly IRazorMappingService _razorMappingService = razorMappingService;

    public bool SupportsMappingImportDirectives => true;

    public async Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
        Document oldDocument,
        Document newDocument,
        CancellationToken cancellationToken)
    {
        var mappedEdits = await _razorMappingService.MapTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);

        var changesCount = mappedEdits.Select(e => e.IsDefault ? 0 : e.TextChanges.Length).Sum();
        var changes = new (string mappedFilePath, TextChange mappedTextChange)[changesCount];

        var i = 0;
        foreach (var mappedEdit in mappedEdits)
        {
            if (mappedEdit.IsDefault)
            {
                continue;
            }

            foreach (var textChange in mappedEdit.TextChanges)
            {
                changes[i++] = (mappedEdit.FilePath, textChange);
            }
        }

        Debug.Assert(i == changesCount);
        return changes.ToImmutableArray();
    }

    public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(
        Document document,
        IEnumerable<TextSpan> spans,
        CancellationToken cancellationToken)
    {
        var razorSpans = await _razorMappingService.MapSpansAsync(document, spans, cancellationToken).ConfigureAwait(false);
        var roslynSpans = new MappedSpanResult[spans.Count()];

        if (roslynSpans.Length != razorSpans.Length)
        {
            // Span mapping didn't succeed. Razor can log telemetry but this still needs to be handled so return all defaults
            // to indicate mapping didn't succeed.
            return roslynSpans.ToImmutableArray();
        }

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
