// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// Handle that can be used with <see cref="IChecksummedPersistentStorage"/> to read data for a
    /// <see cref="Project"/> without needing to have the entire <see cref="Project"/> snapshot available.
    /// This is useful for cases where acquiring an entire snapshot might be expensive (for example, during 
    /// solution load), but querying the data is still desired.
    /// </summary>
    [DataContract]
    internal readonly struct ProjectKey(SolutionKey solution, ProjectId id, string? filePath, string name, Checksum parseOptionsChecksum)
    {
        [DataMember(Order = 0)]
        public readonly SolutionKey Solution = solution;

        [DataMember(Order = 1)]
        public readonly ProjectId Id = id;

        [DataMember(Order = 2)]
        public readonly string? FilePath = filePath;

        [DataMember(Order = 3)]
        public readonly string Name = name;

        [DataMember(Order = 4)]
        public readonly Checksum ParseOptionsChecksum = parseOptionsChecksum;

        public static ProjectKey ToProjectKey(Project project)
            => ToProjectKey(project.Solution.State, project.State);

        public static ProjectKey ToProjectKey(SolutionState solutionState, ProjectState projectState)
            => ToProjectKey(SolutionKey.ToSolutionKey(solutionState), projectState);

        public static ProjectKey ToProjectKey(SolutionKey solutionKey, ProjectState projectState)
            => new(solutionKey, projectState.Id, projectState.FilePath, projectState.Name, projectState.GetParseOptionsChecksum());
    }
}
