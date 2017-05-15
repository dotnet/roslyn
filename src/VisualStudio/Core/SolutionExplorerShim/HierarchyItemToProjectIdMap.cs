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

        public bool TryGetProjectId(IVsHierarchyItem hierarchyItem, string targetFrameworkMoniker, out ProjectId projectId)
        {
            if (_workspace.DeferredState == null)
            {
                projectId = default(ProjectId);
                return false;
            }

            // A project node is represented in two different hierarchies: the solution's IVsHierarchy (where it is a leaf node)
            // and the project's own IVsHierarchy (where it is the root node). The IVsHierarchyItem joins them together for the
            // purpose of creating the tree displayed in Solution Explorer. The project's hierarchy is what is passed from the
            // project system to the language service, so that's the one the one to query here. To do that we need to get
            // the "nested" hierarchy from the IVsHierarchyItem.
            var nestedHierarchy = hierarchyItem.HierarchyIdentity.NestedHierarchy;
            var nestedHierarchyId = hierarchyItem.HierarchyIdentity.NestedItemID;

            if (!nestedHierarchy.TryGetCanonicalName(nestedHierarchyId, out string nestedCanonicalName)
                || !nestedHierarchy.TryGetItemName(nestedHierarchyId, out string nestedName))
            {
                projectId = default(ProjectId);
                return false;
            }

            var project = _workspace.DeferredState.ProjectTracker.ImmutableProjects
                    .Where(p =>
                    {
                        // We're about to access various properties of the IVsHierarchy associated with the project.
                        // The properties supported and the interpretation of their values varies from one project system
                        // to another. This code is designed with C# and VB in mind, so we need to filter out everything
                        // else.
                        if (p.Language != LanguageNames.CSharp
                            && p.Language != LanguageNames.VisualBasic)
                        {
                            return false;
                        }

                        // Here we try to match the hierarchy from Solution Explorer to a hierarchy from the Roslyn project.
                        // The canonical name of a hierarchy item must be unique _within_ an hierarchy, but since we're
                        // examining multiple hierarchies the canonical name could be the same. Indeed this happens when two
                        // project files are in the same folder--they both use the full path to the _folder_ as the canonical
                        // name. To distinguish them we also examine the "regular" name, which will necessarily be different
                        // if the two projects are in the same folder.
                        if (p.Hierarchy.TryGetCanonicalName((uint)VSConstants.VSITEMID.Root, out string projectCanonicalName)
                            && p.Hierarchy.TryGetItemName((uint)VSConstants.VSITEMID.Root, out string projectName)
                            && projectCanonicalName.Equals(nestedCanonicalName, System.StringComparison.OrdinalIgnoreCase)
                            && projectName.Equals(nestedName))
                        {
                            if (targetFrameworkMoniker == null)
                            {
                                return true;
                            }

                            return p.Hierarchy.TryGetTargetFrameworkMoniker((uint)VSConstants.VSITEMID.Root, out string projectTargetFrameworkMoniker)
                                && projectTargetFrameworkMoniker.Equals(targetFrameworkMoniker);
                        }

                        return false;
                    })
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
