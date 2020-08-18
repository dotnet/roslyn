// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// checksum scope that one can use to pin assets in memory while working on remote host
    /// </summary>
    internal sealed class PinnedRemotableDataScope : IDisposable
    {
        private readonly AssetStorages _storages;
        private bool _disposed;

        public readonly PinnedSolutionInfo SolutionInfo;

        public PinnedRemotableDataScope(
            AssetStorages storages,
            PinnedSolutionInfo solutionInfo)
        {
            _storages = storages;
            SolutionInfo = solutionInfo;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _storages.UnregisterSnapshot(SolutionInfo.ScopeId);
            }

            GC.SuppressFinalize(this);
        }

        ~PinnedRemotableDataScope()
        {
            if (!Environment.HasShutdownStarted)
            {
                Contract.Fail($@"Should have been disposed!");
            }
        }
    }
}
