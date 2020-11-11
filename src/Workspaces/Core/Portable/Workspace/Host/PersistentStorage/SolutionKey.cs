// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PersistentStorage
{
    /// <summary>
    /// Handle that can be used with <see cref="IChecksummedPersistentStorage"/> to read data for a
    /// <see cref="Solution"/> without needing to have the entire <see cref="Solution"/> snapshot available.
    /// This is useful for cases where acquiring an entire snapshot might be expensive (for example, during 
    /// solution load), but querying the data is still desired.
    /// </summary>
    internal readonly struct SolutionKey
    {
        public readonly SolutionId Id;
        public readonly string FilePath;
        public readonly bool IsPrimaryBranch;

        public SolutionKey(SolutionId id, string filePath, bool isPrimaryBranch)
        {
            Id = id;
            FilePath = filePath;
            IsPrimaryBranch = isPrimaryBranch;
        }

        public static explicit operator SolutionKey(Solution solution)
            => (SolutionKey)solution.State;

        public static explicit operator SolutionKey(SolutionState solutionState)
            => new(solutionState.Id, solutionState.FilePath, solutionState.BranchId == solutionState.Workspace.PrimaryBranchId);

        public SerializableSolutionKey Dehydrate()
            => new(Id, FilePath, IsPrimaryBranch);
    }

    [DataContract]
    internal readonly struct SerializableSolutionKey
    {
        [DataMember(Order = 0)]
        public readonly SolutionId Id;

        [DataMember(Order = 1)]
        public readonly string FilePath;

        [DataMember(Order = 2)]
        public readonly bool IsPrimaryBranch;

        public SerializableSolutionKey(SolutionId id, string filePath, bool isPrimaryBranch)
        {
            Id = id;
            FilePath = filePath;
            IsPrimaryBranch = isPrimaryBranch;
        }

        public SolutionKey Rehydrate()
            => new(Id, FilePath, IsPrimaryBranch);
    }
}
