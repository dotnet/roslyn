// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SpellCheck;

namespace Microsoft.CodeAnalysis.Remote.Razor.SpellCheck;

[Export(typeof(ICSharpSpellCheckRangeProvider)), Shared]
[method: ImportingConstructor]
internal sealed class CSharpSpellCheckRangeProvider() : ICSharpSpellCheckRangeProvider
{
    public async Task<ImmutableArray<SpellCheckRange>> GetCSharpSpellCheckRangesAsync(RemoteDocumentSnapshot snapshot, CancellationToken cancellationToken)
    {
        using var ranges = new PooledArrayBuilder<SpellCheckRange>();

        // Spell-check spans can come from either generated C# document. Keep track of which document produced each range
        // so the service can map it back through the matching source mappings later.
        var implGeneratedDocument = await snapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        var implCsharpRanges = await GetSpellCheckSpansAsync(implGeneratedDocument, cancellationToken).ConfigureAwait(false);
        foreach (var range in implCsharpRanges)
        {
            ranges.Add(new((int)range.Kind, range.StartIndex, range.Length, InDeclDocument: false));
        }

        if (await snapshot.TryGetGeneratedDocumentAsync(declarationDocument: true, cancellationToken).ConfigureAwait(false) is { } declGeneratedDocument)
        {
            var declCsharpRanges = await GetSpellCheckSpansAsync(declGeneratedDocument, cancellationToken).ConfigureAwait(false);
            foreach (var range in declCsharpRanges)
            {
                ranges.Add(new((int)range.Kind, range.StartIndex, range.Length, InDeclDocument: true));
            }
        }

        return ranges.ToImmutable();
    }

    private static async Task<ImmutableArray<SpellCheckSpan>> GetSpellCheckSpansAsync(Document document, CancellationToken cancellationToken)
    {
        var service = document.GetLanguageService<ISpellCheckSpanService>();
        if (service is null)
        {
            return [];
        }

        var spans = await service.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);

        return spans.SelectAsArray(static span =>
            new SpellCheckSpan(
                span.TextSpan.Start,
                span.TextSpan.Length,
                ProtocolConversions.SpellCheckSpanKindToSpellCheckableRangeKind(span.Kind)));
    }

    private readonly record struct SpellCheckSpan(int StartIndex, int Length, VSInternalSpellCheckableRangeKind Kind);
}
