// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IAssemblyPathResolver : IWorkspaceService
    {
        /// <summary>
        /// Returns null if the assembly name could not be resolved.
        /// </summary>
        string ResolveAssemblyPath(ProjectId project, string assemblyName);
    }
}
