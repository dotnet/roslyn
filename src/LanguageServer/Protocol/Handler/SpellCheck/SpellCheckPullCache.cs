// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SpellCheck;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SpellCheck;

internal record struct SpellCheckState(ISpellCheckSpanService Service, Document Document);

/// <summary>
/// Simplified version of <see cref="VersionedPullCache{TVersion, TState, TComputedData}"/> that only uses a 
/// single cheap key to check results against.
/// </summary>
internal sealed class SpellCheckPullCache(string uniqueKey) : VersionedPullCache<(Checksum parseOptionsChecksum, Checksum textChecksum), SpellCheckState, ImmutableArray<SpellCheckSpan>>(uniqueKey)
{
    public override async Task<(Checksum parseOptionsChecksum, Checksum textChecksum)> ComputeVersionAsync(SpellCheckState state, CancellationToken cancellationToken)
    {
        var project = state.Document.Project;
        var parseOptionsChecksum = project.State.GetParseOptionsChecksum();

        var documentChecksumState = await state.Document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
        var textChecksum = documentChecksumState.Text;

        return (parseOptionsChecksum, textChecksum);
    }

    public override Checksum ComputeChecksum(ImmutableArray<SpellCheckSpan> data, string language)
    {
        var checksums = data.SelectAsArray(s => Checksum.Create(s, SerializeSpellCheckSpan)).Sort();
        return Checksum.Create(checksums);
    }

    public override async Task<ImmutableArray<SpellCheckSpan>> ComputeDataAsync(SpellCheckState state, CancellationToken cancellationToken)
    {
        var spans = await state.Service.GetSpansAsync(state.Document, cancellationToken).ConfigureAwait(false);
        return spans;
    }

    private void SerializeSpellCheckSpan(SpellCheckSpan span, ObjectWriter writer)
    {
        writer.WriteInt32(span.TextSpan.Start);
        writer.WriteInt32(span.TextSpan.Length);
        writer.WriteInt32((int)span.Kind);
    }
}
