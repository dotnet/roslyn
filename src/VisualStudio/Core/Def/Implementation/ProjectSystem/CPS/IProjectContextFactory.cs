// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Factory to create a project context for a new Workspace project that can be initialized on a background thread.
    /// </summary>
    internal interface IProjectContextFactory
    {
        /// <summary>
        /// Creates and initializes a new Workspace project and returns a <see cref="IProjectContext"/> to lazily initialize the properties and items for the project.
        /// This method can be invoked on a background thread and doesn't access any members of the given UI <paramref name="hierarchy"/>,
        /// allowing the UI hierarchy to be published lazily.
        /// </summary>
        /// <param name="languageName">Project language.</param>
        /// <param name="projectDisplayName">Display name for the project.</param>
        /// <param name="projectFilePath">Full path to the project file for the project.</param>
        /// <param name="projectGuid">Project <see cref="Guid"/>. Guid can also be initialized lazily with <see cref="IProjectContext.Guid"/>.</param>
        /// <param name="projectTypeGuid">String representing the Guid for the Project type. Project type can also be initialized lazily with <see cref="IProjectContext.ProjectType"/>.</param>
        /// <param name="hierarchy"><see cref="IVsHierarchy"/> for the project.</param>
        /// <param name="commandLineArguments">Initial command line arguments to initialize the compilation and parse options for the project.</param>
        IProjectContext CreateProjectContext(string languageName, string projectDisplayName, string projectFilePath, Guid projectGuid, string projectTypeGuid, IVsHierarchy hierarchy, CommandLineArguments commandLineArguments);
    }
}
