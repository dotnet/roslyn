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
        /// This indicates a Solution.WorkspaceVersion of this solution. remote host engine uses this version
        /// to decide whether caching this solution will benefit other requests or not
        /// </summary>
        [DataMember(Order = 1)]
        public readonly int WorkspaceVersion;

        [DataMember(Order = 2)]
        public readonly Checksum SolutionChecksum;

        /// <summary>
        /// An optional project that we are pinning information for.  This is used for features that only need
        /// information for a project (and its dependencies) and not the entire solution.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly ProjectId? ProjectId;

        public PinnedSolutionInfo(
            int scopeId,
            int workspaceVersion,
            Checksum solutionChecksum,
            ProjectId? projectId)
        {
            ScopeId = scopeId;
            WorkspaceVersion = workspaceVersion;
            SolutionChecksum = solutionChecksum;
            ProjectId = projectId;
        }
    }
}
