// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
