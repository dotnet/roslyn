// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// Maps from hierarchy items to project IDs.
    /// </summary>
    internal interface IHierarchyItemToProjectIdMap : IWorkspaceService
    {
        /// <summary>
        /// Given an <see cref="IVsHierarchyItem"/> representing a project and an optional target framework moniker,
        /// returns the <see cref="ProjectId"/> of the equivalent Roslyn <see cref="Project"/>.
        /// </summary>
        /// <param name="hierarchyItem">An <see cref="IVsHierarchyItem"/> for the project root.</param>
        /// <param name="targetFrameworkMoniker">An optional string representing a TargetFrameworkMoniker.
        /// This is only useful in multi-targeting scenarios where there may be multiple Roslyn projects 
        /// (one per target framework) for a single project on disk.</param>
        /// <param name="projectId">The <see cref="ProjectId"/> of the found project, if any.</param>
        /// <returns>True if the desired project was found; false otherwise.</returns>
        bool TryGetProjectId(IVsHierarchyItem hierarchyItem, string targetFrameworkMoniker, out ProjectId projectId);
    }
}
