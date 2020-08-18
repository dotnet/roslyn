// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class AssetStorages
    {
        internal readonly struct Scope : IDisposable
        {
            private readonly AssetStorages _storages;
            public readonly PinnedSolutionInfo SolutionInfo;

            public Scope(AssetStorages storages, PinnedSolutionInfo solutionInfo)
            {
                _storages = storages;
                SolutionInfo = solutionInfo;
            }

            public void Dispose()
            {
                _storages.RemoveScope(SolutionInfo.ScopeId);
            }
        }
    }
}
