// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class SolutionAssetStorage
    {
        internal class Scope : IDisposable
        {
            private readonly SolutionAssetStorage _storage;

            public readonly Checksum Checksum;
            public readonly PinnedSolutionInfo SolutionInfo;

            public Scope(SolutionAssetStorage storage, Checksum checksum, PinnedSolutionInfo solutionInfo)
            {
                _storage = storage;
                Checksum = checksum;
                SolutionInfo = solutionInfo;
            }

            public void Dispose()
                => _storage.DisposeScope(this);
        }
    }
}
