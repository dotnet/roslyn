// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides a way to map from an assembly name to the actual path of the .NET Framework 
    /// assemby with that name in the context of a specified project.  For example, if the 
    /// assembly name is "System.Data" then a project targetting .NET 2.0 would resolve this
    /// to a different path than a project targetting .NET 4.5.
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
        string ResolveAssemblyPath(ProjectId projectId, string assemblyName, string fullyQualifiedName = null);

        // bool CanResolveType(ProjectId projectId, string assemblyName, string fullyQualifiedTypeName);
    }
}
