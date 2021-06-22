// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    /// <summary>
    /// Factory to create a project context for a new Workspace project that can be initialized on a background thread.
    /// </summary>
    internal interface IWorkspaceProjectContextFactory
    {
        /// <inheritdoc cref="CreateProjectContextAsync"/>
        [Obsolete("Use CreateProjectContextAsync instead")]
        IWorkspaceProjectContext CreateProjectContext(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid, object? hierarchy, string? binOutputPath);

        /// <inheritdoc cref="CreateProjectContextAsync"/>
        [Obsolete("Use CreateProjectContextAsync instead")]
        IWorkspaceProjectContext CreateProjectContext(string languageName, string projectUniqueName, string projectFilePath, Guid projectGuid, object? hierarchy, string? binOutputPath, string? assemblyName);

        /// <summary>
        /// Creates and initializes a new Workspace project and returns a <see
        /// cref="IWorkspaceProjectContext"/> to lazily initialize the properties and items for the
        /// project.  This method guarantees that either the project is added (and the returned task
        /// completes) or cancellation is observed and no project is added.
        /// </summary>
        /// <param name="languageName">Project language.</param>
        /// <param name="projectUniqueName">Unique name for the project.</param>
        /// <param name="projectFilePath">Full path to the project file for the project.</param>
        /// <param name="projectGuid">Project guid.</param>
        /// <param name="hierarchy">The IVsHierarchy for the project; this is used to track linked files across multiple projects when determining contexts.</param>
        /// <param name="binOutputPath">Initial project binary output path.</param>
        Task<IWorkspaceProjectContext> CreateProjectContextAsync(
            string languageName,
            string projectUniqueName,
            string projectFilePath,
            Guid projectGuid,
            object? hierarchy,
            string? binOutputPath,
            string? assemblyName,
            CancellationToken cancellationToken);
    }
}
