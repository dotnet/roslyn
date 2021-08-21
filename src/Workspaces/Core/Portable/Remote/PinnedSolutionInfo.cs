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
        /// Unique ID for this pinned solution
        /// 
        /// This later used to find matching solution between VS and remote host
        /// </summary>
        [DataMember(Order = 0)]
        public readonly int ScopeId;

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

        [DataMember(Order = 3)]
        public readonly Checksum SolutionChecksum;

        public PinnedSolutionInfo(int scopeId, bool fromPrimaryBranch, int workspaceVersion, Checksum solutionChecksum)
        {
            ScopeId = scopeId;
            FromPrimaryBranch = fromPrimaryBranch;
            WorkspaceVersion = workspaceVersion;
            SolutionChecksum = solutionChecksum;
        }
    }
}
