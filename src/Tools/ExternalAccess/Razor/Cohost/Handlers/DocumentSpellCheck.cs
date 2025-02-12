// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SpellCheck;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class SpellCheck
{
    public readonly record struct SpellCheckSpan(int StartIndex, int Length, VSInternalSpellCheckableRangeKind Kind);

    public static async Task<ImmutableArray<SpellCheckSpan>> GetSpellCheckSpansAsync(Document document, CancellationToken cancellationToken)
    {
        var service = document.GetLanguageService<ISpellCheckSpanService>();
        if (service is null)
        {
            return [];
        }

        var spans = await service.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<SpellCheckSpan>.GetInstance(spans.Length, out var razorSpans);
        foreach (var span in spans)
        {
            var kind = ProtocolConversions.SpellCheckSpanKindToSpellCheckableRangeKind(span.Kind);
            razorSpans.Add(new SpellCheckSpan(span.TextSpan.Start, span.TextSpan.Length, kind));
        }

        return razorSpans.ToImmutable();
    }
}
