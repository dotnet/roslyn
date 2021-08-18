﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class SolutionAssetStorage
    {
        internal readonly struct Scope : IDisposable
        {
            private readonly SolutionAssetStorage _storages;
            public readonly PinnedSolutionInfo SolutionInfo;

            public Scope(SolutionAssetStorage storages, PinnedSolutionInfo solutionInfo)
            {
                _storages = storages;
                SolutionInfo = solutionInfo;
            }

            public void Dispose()
            {
                Contract.ThrowIfFalse(_storages._solutionStates.TryRemove(SolutionInfo.ScopeId, out var entry));
                entry.ReplicationContext.Dispose();
            }
        }
    }
}
