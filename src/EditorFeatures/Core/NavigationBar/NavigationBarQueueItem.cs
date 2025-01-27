// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar;

/// <param name="FrozenPartialSemantics">Indicates if we should compute with frozen partial semantics or
/// not.</param>
/// <param name="NonFrozenComputationToken">If <paramref name="FrozenPartialSemantics"/> is false, then this is a
/// cancellation token that can cancel the expensive work being done if new frozen-partial work is
/// requested.</param>
internal readonly record struct NavigationBarQueueItem(
    bool FrozenPartialSemantics,
    CancellationToken? NonFrozenComputationToken);
