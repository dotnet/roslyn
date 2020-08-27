// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public readonly ProjectId Id;
        public readonly string FilePath;
        public readonly string Name;

        public ProjectKey(ProjectId id, string filePath, string name)
        {
            Id = id;
            FilePath = filePath;
            Name = name;
        }

        public static explicit operator ProjectKey(Project project)
            => new ProjectKey(project.Id, project.FilePath, project.Name);
    }
}
