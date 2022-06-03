// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides a way to map from an assembly name to the actual path of the .NET Framework 
    /// assembly with that name in the context of a specified project.  For example, if the 
    /// assembly name is "System.Data" then a project targeting .NET 2.0 would resolve this
    /// to a different path than a project targeting .NET 4.5.
    /// </summary>
    internal interface IFrameworkAssemblyPathResolver : IWorkspaceService
    {
        /// <summary>
        /// Returns null if the assembly name could not be resolved.
        /// </summary>
        /// <param name="fullyQualifiedName">An optional type name for a type that must
        /// exist in the assembly.</param>
        /// <param name="projectId">The project context to search within.</param>
        /// <param name="assemblyName">The name of the assembly to try to resolve.</param>
        string? ResolveAssemblyPath(ProjectId projectId, string assemblyName, string? fullyQualifiedName);

        // bool CanResolveType(ProjectId projectId, string assemblyName, string fullyQualifiedTypeName);
    }
}
