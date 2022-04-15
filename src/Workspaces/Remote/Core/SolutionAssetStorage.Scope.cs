// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class SolutionAssetStorage
    {
        internal sealed class Scope : IDisposable
        {
            private readonly SolutionAssetStorage _storage;

            public readonly Checksum Checksum;
            public readonly PinnedSolutionInfo SolutionInfo;
            public readonly SolutionState Solution;

            /// <summary>
            ///  Will be disposed from <see cref="SolutionAssetStorage.DecreaseScopeRefCount(Scope)"/> when the last
            ///  ref-count to this scope goes away.
            /// </summary>
            public readonly SolutionReplicationContext ReplicationContext = new();

            /// <summary>
            /// Only safe to read write while <see cref="_gate"/> is held.
            /// </summary>
            public int RefCount = 1;

            public Scope(
                SolutionAssetStorage storage,
                Checksum checksum,
                PinnedSolutionInfo solutionInfo,
                SolutionState solution)
            {
                _storage = storage;
                Checksum = checksum;
                SolutionInfo = solutionInfo;
                Solution = solution;
            }

            public void Dispose()
                => _storage.DecreaseScopeRefCount(this);
        }
    }
}
