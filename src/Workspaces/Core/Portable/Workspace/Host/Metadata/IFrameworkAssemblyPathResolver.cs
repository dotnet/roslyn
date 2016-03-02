// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides a way to map from an assembly name to the actual path of the .Net framework 
    /// assemby with that name in the context of a specified project.  For example, if the 
    /// assembly name is "System.Data" then a project targetting .Net 2.0 would resolve this
    /// to a different path than a project targetting .Net 4.5.
    /// </summary>
    internal interface IFrameworkAssemblyPathResolver : IWorkspaceService
    {
        /// <summary>
        /// Returns null if the assembly name could not be resolved.
        /// </summary>
        string ResolveAssemblyPath(ProjectId project, string assemblyName);
    }
}
