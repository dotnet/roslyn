// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal partial class AbstractAsynchronousTaggerProvider<TTag>
{
    /// <param name="HighPriority">Specifies if this is the initial set of tags being computed or not, and no
    /// artificial delays should be inserted when computing the tags.</param>
    /// <param name="FrozenPartialSemantics">Indicates if we should
    /// compute with frozen partial semantics or not.</param>
    /// <param name="NonFrozenComputationToken">If <paramref name="FrozenPartialSemantics"/> is false, and this
    /// queue does support computing frozen partial semantics (see <see cref="SupportsFrozenPartialSemantics"/>)
    /// then this is a cancellation token that can cancel the expensive work being done if new frozen-partial work
    /// is requested.</param>
    private record struct TagSourceQueueItem(bool HighPriority, bool FrozenPartialSemantics, CancellationToken? NonFrozenComputationToken);
}
