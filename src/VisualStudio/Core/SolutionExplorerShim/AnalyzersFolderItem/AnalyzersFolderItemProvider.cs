// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        internal AnalyzersFolderItemProvider(IHierarchyItemToProjectIdMap projectMap, Workspace workspace, IAnalyzersCommandHandler commandHandler)
        {
            _projectMap = projectMap;
            _workspace = workspace;
            _commandHandler = commandHandler;
        }

        protected override IAttachedCollectionSource CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
        {
            if (item != null &&
                item.HierarchyIdentity != null &&
                item.HierarchyIdentity.NestedHierarchy != null &&
                relationshipName == KnownRelationships.Contains)
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
