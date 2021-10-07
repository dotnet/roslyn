// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// solution branch Id
    /// </summary>
    [DebuggerDisplay($"{nameof(_id)}")]
    internal class BranchId
    {
        /// <summary>
        /// Used only if <see cref="WorkspaceExperiment.DisableBranchId"/> is set.
        /// </summary>
        private static readonly BranchId s_experimentSingleton = new(0);

        private static int s_nextId = 1;

        private readonly int _id;

        private BranchId(int id)
            => _id = id;

        internal static BranchId GetNextId(bool disableBranchIds)
            => disableBranchIds ? s_experimentSingleton : new(Interlocked.Increment(ref s_nextId));
    }
}
