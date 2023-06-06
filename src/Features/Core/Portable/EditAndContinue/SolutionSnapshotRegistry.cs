// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Contracts.Client;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal interface ISolutionSnapshotRegistry
{
    SolutionSnapshotId RegisterSolutionSnapshot(Solution solution);
}

[Shared]
[Export(typeof(SolutionSnapshotRegistry))]
[Export(typeof(ISolutionSnapshotRegistry))]
internal sealed class SolutionSnapshotRegistry : ISolutionSnapshotRegistry
{
    private static int s_solutionSnapshotId;

    // lock on access
    private readonly Dictionary<SolutionSnapshotId, Solution> _pendingSolutionSnapshots = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SolutionSnapshotRegistry()
    {
    }

    /// <summary>
    /// Called from LSP server.
    /// </summary>
    public SolutionSnapshotId RegisterSolutionSnapshot(Solution solution)
    {
        var id = new SolutionSnapshotId(Interlocked.Increment(ref s_solutionSnapshotId));

        lock (_pendingSolutionSnapshots)
        {
            _pendingSolutionSnapshots.Add(id, solution);
        }

        return id;
    }

    public Solution GetRegisteredSolutionSnapshot(SolutionSnapshotId id)
    {
        lock (_pendingSolutionSnapshots)
        {
            Contract.ThrowIfFalse(_pendingSolutionSnapshots.TryGetValue(id, out var solution));
            Contract.ThrowIfFalse(_pendingSolutionSnapshots.Remove(id));
            return solution;
        }
    }

    public void Clear()
    {
        lock (_pendingSolutionSnapshots)
        {
            _pendingSolutionSnapshots.Clear();
        }
    }
}
