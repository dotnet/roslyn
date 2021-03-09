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
    /// <see cref="Project"/> without needing to have the entire <see cref="Project"/> snapshot available.
    /// This is useful for cases where acquiring an entire snapshot might be expensive (for example, during 
    /// solution load), but querying the data is still desired.
    /// </summary>
    internal readonly struct ProjectKey
    {
        public readonly SolutionKey Solution;

        public readonly ProjectId Id;
        public readonly string FilePath;
        public readonly string Name;
        public readonly Checksum ParseOptionsChecksum;

        public ProjectKey(SolutionKey solution, ProjectId id, string filePath, string name, Checksum parseOptionsChecksum)
        {
            Solution = solution;
            Id = id;
            FilePath = filePath;
            Name = name;
            ParseOptionsChecksum = parseOptionsChecksum;
        }

        public static ProjectKey ToProjectKey(Project project)
            => ToProjectKey(project.Solution.State, project.State);

        public static ProjectKey ToProjectKey(SolutionState solutionState, ProjectState projectState)
            => new(SolutionKey.ToSolutionKey(solutionState), projectState.Id, projectState.FilePath, projectState.Name, projectState.GetParseOptionsChecksum());

        public SerializableProjectKey Dehydrate()
            => new(Solution.Dehydrate(), Id, FilePath, Name, ParseOptionsChecksum);
    }

    [DataContract]
    internal readonly struct SerializableProjectKey
    {
        [DataMember(Order = 0)]
        public readonly SerializableSolutionKey Solution;

        [DataMember(Order = 1)]
        public readonly ProjectId Id;

        [DataMember(Order = 2)]
        public readonly string FilePath;

        [DataMember(Order = 3)]
        public readonly string Name;

        [DataMember(Order = 4)]
        public readonly Checksum ParseOptionsChecksum;

        public SerializableProjectKey(SerializableSolutionKey solution, ProjectId id, string filePath, string name, Checksum parseOptionsChecksum)
        {
            Solution = solution;
            Id = id;
            FilePath = filePath;
            Name = name;
            ParseOptionsChecksum = parseOptionsChecksum;
        }

        public ProjectKey Rehydrate()
            => new(Solution.Rehydrate(), Id, FilePath, Name, ParseOptionsChecksum);
    }
}
