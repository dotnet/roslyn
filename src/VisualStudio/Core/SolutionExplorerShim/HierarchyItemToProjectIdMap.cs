// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [ExportWorkspaceService(typeof(IHierarchyItemToProjectIdMap), ServiceLayer.Host), Shared]
    internal class HierarchyItemToProjectIdMap : IHierarchyItemToProjectIdMap
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        public HierarchyItemToProjectIdMap(VisualStudioWorkspaceImpl workspace)
        {
            _workspace = workspace;
        }

        public bool TryGetProjectId(IVsHierarchyItem hierarchyItem, out ProjectId projectId)
        {
            var project = _workspace.ProjectTracker.ImmutableProjects
                    .Where(p => p.Hierarchy == hierarchyItem.HierarchyIdentity.NestedHierarchy)
                    .Where(p => p.ProjectSystemName == hierarchyItem.CanonicalName)
                    .SingleOrDefault();

            if (project == null)
            {
                projectId = default(ProjectId);
                return false;
            }
            else
            {
                projectId = project.Id;
                return true;
            }
        }
    }
}
