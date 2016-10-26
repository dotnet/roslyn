// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provide host specific information if host supports it.
    /// </summary>
    internal interface IProjectTypeLookupService : IWorkspaceService
    {
        string GetProjectType(Workspace workspace, ProjectId projectId);
    }
}
