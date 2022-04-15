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

        public PinnedSolutionInfo(
            Checksum solutionChecksum)
        {
            SolutionChecksum = solutionChecksum;
        }
    }
}
