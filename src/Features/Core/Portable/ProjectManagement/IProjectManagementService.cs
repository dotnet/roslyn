// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ProjectManagement
{
    /// <summary>
    /// This service provides a way to extract all the folders under a given project, or find the default namespace if it exists.
    /// </summary>
    internal interface IProjectManagementService : IWorkspaceService
    {
        // Returns the list of all the folders under the given project
        IList<string> GetFolders(ProjectId projectId, Workspace workspace);

        // Returns the DefaultNamespace if present else returns an empty string
        string GetDefaultNamespace(Project project, Workspace workspace);
    }
}
