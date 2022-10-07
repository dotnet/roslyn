// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteWorkspace
    {
        /// <summary>
        /// Corresponds to a solution that is being used in the remote workspace and has been 'pinned' so that it will
        /// not be released.  While pinned any concurrent calls into the remote workspace for the same solution checksum
        /// will get the same solution instance.  Note: services should almost always use <see
        /// cref="RunWithSolutionAsync{T}(AssetProvider, Checksum, Func{Solution, ValueTask{T}}, CancellationToken)"/>
        /// instead of calling <see cref="GetPinnedSolutionAsync(AssetProvider, Checksum, CancellationToken)"/>.  The
        /// former ensures that the ref-counts around the pinned solution are properly handled.  If <see
        /// cref="GetPinnedSolutionAsync(AssetProvider, Checksum, CancellationToken)"/> is used, great care must be
        /// followed to ensure it is properly disposed so that data is not held around indefinitely.
        /// </summary>
        public sealed class PinnedSolution : System.IAsyncDisposable
        {
            private readonly RemoteWorkspace _workspace;
            private readonly InFlightSolution _inFlightSolution;
            public readonly Solution Solution;

            public PinnedSolution(RemoteWorkspace workspace, InFlightSolution inFlightSolution, Solution solution)
            {
                _workspace = workspace;
                _inFlightSolution = inFlightSolution;
                Solution = solution;
            }

            public async ValueTask DisposeAsync()
            {
                await _workspace.DecrementInFlightCountAsync(_inFlightSolution).ConfigureAwait(false);
            }
        }
    }
}
