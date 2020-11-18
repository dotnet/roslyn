// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => new SolutionKey(solution.Id, solution.FilePath, solution.BranchId == solution.Workspace.PrimaryBranchId);

        public SerializableSolutionKey Dehydrate()
        {
            return new SerializableSolutionKey
            {
                Id = Id,
                FilePath = FilePath,
                IsPrimaryBranch = IsPrimaryBranch,
            };
        }
    }

    internal class SerializableSolutionKey
    {
        public SolutionId Id;
        public string FilePath;
        public bool IsPrimaryBranch;

        public SolutionKey Rehydrate()
            => new SolutionKey(Id, FilePath, IsPrimaryBranch);
    }
}
