// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name("AnalyzersFolderProvider")]
    [Order(Before = HierarchyItemsProviderNames.Contains)]
    internal class AnalyzersFolderItemProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
    {
        // NOTE: the IComponentModel is used here rather than importing ISolutionExplorerWorkspaceProvider directly
        // to avoid loading VisualStudioWorkspace and dependent assemblies directly
        private readonly IComponentModel _componentModel;
        private readonly IAnalyzersCommandHandler _commandHandler;
        private IHierarchyItemToProjectIdMap _projectMap;
        private Workspace _workspace;

        [ImportingConstructor]
        public AnalyzersFolderItemProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler)
        {
            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            _commandHandler = commandHandler;
        }

        /// <summary>
        /// Constructor for use only in unit tests. Bypasses MEF to set the project mapper, workspace and glyph service.
        /// </summary>
        internal AnalyzersFolderItemProvider(IHierarchyItemToProjectIdMap projectMap, Workspace workspace, IAnalyzersCommandHandler commandHandler)
        {
            _projectMap = projectMap;
            _workspace = workspace;
            _commandHandler = commandHandler;
        }

        protected override IAttachedCollectionSource CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
        {
            if (item is { HierarchyIdentity: { NestedHierarchy: { } } } && relationshipName is KnownRelationships.Contains)
            {
                var hierarchy = item.HierarchyIdentity.NestedHierarchy;
                var itemId = item.HierarchyIdentity.NestedItemID;

                var projectTreeCapabilities = GetProjectTreeCapabilities(hierarchy, itemId);
                if (projectTreeCapabilities.Any(c => c.Equals("References")))
                {
                    return CreateCollectionSourceCore(item.Parent, item);
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IAttachedCollectionSource CreateCollectionSourceCore(IVsHierarchyItem parentItem, IVsHierarchyItem item)
        {
            var hierarchyMapper = TryGetProjectMap();
            if (hierarchyMapper != null &&
                hierarchyMapper.TryGetProjectId(parentItem, targetFrameworkMoniker: null, projectId: out var projectId))
            {
                var workspace = TryGetWorkspace();
                return new AnalyzersFolderItemSource(workspace, projectId, item, _commandHandler);
            }

            return null;
        }

        private static ImmutableArray<string> GetProjectCapabilities(IVsHierarchy hierarchy)
        {
            if (hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID5.VSHPROPID_ProjectCapabilities, out var capabilitiesObj) == VSConstants.S_OK)
            {
                var capabilitiesString = (string)capabilitiesObj;
                return ImmutableArray.Create(capabilitiesString.Split(' '));
            }
            else
            {
                return ImmutableArray<string>.Empty;
            }
        }

        private static ImmutableArray<string> GetProjectTreeCapabilities(IVsHierarchy hierarchy, uint itemId)
        {
            if (hierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out var capabilitiesObj) == VSConstants.S_OK)
            {
                var capabilitiesString = (string)capabilitiesObj;
                return ImmutableArray.Create(capabilitiesString.Split(' '));
            }
            else
            {
                return ImmutableArray<string>.Empty;
            }
        }

        private Workspace TryGetWorkspace()
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
