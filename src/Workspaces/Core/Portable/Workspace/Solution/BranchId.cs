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

        /// <summary>
        /// Whether or not branch-ids are disabled.  Set when the first caller attempts to
        /// get a branch id.  From that point on, this is the value that will be used.  This
        /// way we don't get different behavior at some arbitrary point in time if the flag
        /// changes in the middle of a VS session.
        /// </summary>
        private static bool? s_disabled;
        private static readonly object s_gate = new();

        private static int s_nextId = 1;

        private readonly int _id;

        private BranchId(int id)
            => _id = id;

        internal static BranchId GetNextId(bool tryDisableBranchId)
        {
            lock (s_gate)
            {
                if (s_disabled == null)
                    s_disabled = tryDisableBranchId;

                var disabled = s_disabled.Value;
                return disabled ? s_experimentSingleton : new BranchId(s_nextId++);
            }
        }
    }
}
