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
/// Simplified version of <see cref="VersionedPullCache{TCheapVersion, TExpensiveVersion, TState, TComputedData}"/> that only uses a 
/// single cheap key to check results against.
/// </summary>
internal class SpellCheckPullCache(string uniqueKey) : VersionedPullCache<(Checksum parseOptionsChecksum, Checksum textChecksum)?, object?, SpellCheckState, SpellCheckSpan>(uniqueKey)
{
    public override async Task<(Checksum parseOptionsChecksum, Checksum textChecksum)?> ComputeCheapVersionAsync(SpellCheckState state, CancellationToken cancellationToken)
    {
        var project = state.Document.Project;
        var parseOptionsChecksum = project.State.GetParseOptionsChecksum();

        var documentChecksumState = await state.Document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
        var textChecksum = documentChecksumState.Text;

        return (parseOptionsChecksum, textChecksum);
    }

    public override async Task<ImmutableArray<SpellCheckSpan>> ComputeDataAsync(SpellCheckState state, CancellationToken cancellationToken)
    {
        var spans = await state.Service.GetSpansAsync(state.Document, cancellationToken).ConfigureAwait(false);
        return spans;
    }

    public override Task<object?> ComputeExpensiveVersionAsync(SpellCheckState state, CancellationToken cancellationToken)
    {
        return SpecializedTasks.Null<object>();
    }
}
