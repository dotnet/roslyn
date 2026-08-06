// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SpellCheck;

namespace Microsoft.CodeAnalysis.Remote.Razor.SpellCheck;

[Export(typeof(ICSharpSpellCheckRangeProvider)), Shared]
[method: ImportingConstructor]
internal sealed class CSharpSpellCheckRangeProvider() : ICSharpSpellCheckRangeProvider
{
    public async Task<ImmutableArray<SpellCheckRange>> GetCSharpSpellCheckRangesAsync(RemoteDocumentContext documentContext, CancellationToken cancellationToken)
    {
        // We have a razor document, lets find the generated C# document
        var snapshot = documentContext.Snapshot;
        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        var csharpRanges = await GetSpellCheckSpansAsync(generatedDocument, cancellationToken).ConfigureAwait(false);

        return csharpRanges.SelectAsArray(static r => new SpellCheckRange((int)r.Kind, r.StartIndex, r.Length));
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
