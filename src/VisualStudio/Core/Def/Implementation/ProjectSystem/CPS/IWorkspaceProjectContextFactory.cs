// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Factory to create a project context for a new Workspace project that can be initialized on a background thread.
    /// </summary>
    internal interface IWorkspaceProjectContextFactory
    {
        /// <summary>
        /// Creates and initializes a new Workspace project and returns a <see cref="IWorkspaceProjectContext"/> to lazily initialize the properties and items for the project.
        /// </summary>
        /// <param name="languageName">Project language.</param>
        /// <param name="projectUniqueName">Unique name for the project.</param>
        /// <param name="projectFilePath">Full path to the project file for the project.</param>
        /// <param name="projectGuid">Project guid.</param>
        /// <param name="hierarchy">Obsolete. The argument is ignored.</param>
        /// <param name="binOutputPath">Initial project binary output path.</param>
        IWorkspaceProjectContext CreateProjectContext(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath);
    }
}
