// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract class ProjectFolderItemProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
    {
        private readonly IComponentModel _componentModel;
        private IHierarchyItemToProjectIdMap _projectMap;
        private Workspace _workspace;

        internal ProjectFolderItemProvider(IServiceProvider serviceProvider)
        {
            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
        }

        /// <summary>
        /// Constructor for use only in unit tests. Bypasses MEF.
        /// </summary>
        internal ProjectFolderItemProvider(IHierarchyItemToProjectIdMap projectMap, Workspace workspace)
        {
            _projectMap = projectMap;
            _workspace = workspace;
        }

        protected Workspace TryGetWorkspace()
        {
            if (_workspace == null)
            {
                var provider = _componentModel.DefaultExportProvider.GetExportedValueOrDefault<ISolutionExplorerWorkspaceProvider>();
                if (provider != null)
                {
                    _workspace = provider.GetWorkspace();
                }
            }

            return _workspace;
        }

        protected ProjectId TryGetProject(IVsHierarchyItem item)
        {
            var hierarchyMapper = TryGetProjectMap();
            if (hierarchyMapper == null)
            {
                return null;
            }
            ProjectId projectId;
            hierarchyMapper.TryGetProjectId(item, out projectId);
            return projectId;
        }

        private IHierarchyItemToProjectIdMap TryGetProjectMap()
        {
            var workspace = TryGetWorkspace();
            if (workspace == null)
            {
                return null;
            }

            if (_projectMap == null)
            {
                _projectMap = workspace.Services.GetService<IHierarchyItemToProjectIdMap>();
            }

            return _projectMap;
        }
    }
}
