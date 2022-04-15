// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Information related to pinned solution
    /// </summary>
    [DataContract]
    internal sealed class PinnedSolutionInfo
    {
        /// <summary>
        /// Checksum for the pinned solution. Ensures that OOP synchronization requests can unique identify which
        /// in-flight solution-snapshots they correspond to.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly Checksum SolutionChecksum;

        /// <summary>
        /// This indicates whether this scope is for primary branch or not (not forked solution)
        /// 
        /// Features like OOP will use this flag to see whether caching information related to this solution
        /// can benefit other requests or not
        /// </summary>
        [DataMember(Order = 1)]
        public readonly bool FromPrimaryBranch;

        /// <summary>
        /// This indicates a Solution.WorkspaceVersion of this solution. remote host engine uses this version
        /// to decide whether caching this solution will benefit other requests or not
        /// </summary>
        [DataMember(Order = 2)]
        public readonly int WorkspaceVersion;

        public PinnedSolutionInfo(
            Checksum solutionChecksum,
            bool fromPrimaryBranch,
            int workspaceVersion)
        {
            SolutionChecksum = solutionChecksum;
            FromPrimaryBranch = fromPrimaryBranch;
            WorkspaceVersion = workspaceVersion;
        }
    }
}
