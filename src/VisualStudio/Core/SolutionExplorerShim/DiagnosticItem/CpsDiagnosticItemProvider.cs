// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(CpsDiagnosticItemProvider))]
    [Order]
    internal sealed class CpsDiagnosticItemProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
    {
        private readonly IAnalyzersCommandHandler _commandHandler;
        private readonly IComponentModel _componentModel;

        private IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private IHierarchyItemToProjectIdMap _projectMap;
        private Workspace _workspace;

        [ImportingConstructor]
        public CpsDiagnosticItemProvider(
            [Import(typeof(AnalyzersCommandHandler))]IAnalyzersCommandHandler commandHandler,
            [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
        {
            _commandHandler = commandHandler;
            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
        }

        protected override IAttachedCollectionSource CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
        {
            if (item is { HierarchyIdentity: { NestedHierarchy: { } } } && relationshipName is KnownRelationships.Contains)
            {
                if (NestedHierarchyHasProjectTreeCapability(item, "AnalyzerDependency"))
                {
                    var projectRootItem = FindProjectRootItem(item, out var targetFrameworkMoniker);
                    if (projectRootItem != null)
                    {
                        return CreateCollectionSourceCore(projectRootItem, item, targetFrameworkMoniker);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Starting at the given item, walks up the tree to find the item representing the project root.
        /// If the item is located under a target-framwork specific node, the corresponding 
        /// TargetFrameworkMoniker will be found as well.
        /// </summary>
        private static IVsHierarchyItem FindProjectRootItem(IVsHierarchyItem item, out string targetFrameworkMoniker)
        {
            targetFrameworkMoniker = null;

            for (var parent = item; parent != null; parent = parent.Parent)
            {
                if (targetFrameworkMoniker == null)
                {
                    targetFrameworkMoniker = GetTargetFrameworkMoniker(parent, targetFrameworkMoniker);
                }

                if (NestedHierarchyHasProjectTreeCapability(parent, "ProjectRoot"))
                {
                    return parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Given an item determines if it represents a particular target frmework.
        /// If so, it returns the corresponding TargetFrameworkMoniker.
        /// </summary>
        private static string GetTargetFrameworkMoniker(IVsHierarchyItem item, string targetFrameworkMoniker)
        {
            var hierarchy = item.HierarchyIdentity.NestedHierarchy;
            var itemId = item.HierarchyIdentity.NestedItemID;

            var projectTreeCapabilities = GetProjectTreeCapabilities(hierarchy, itemId);

            var isTargetNode = false;
            string potentialTFM = null;
            foreach (var capability in projectTreeCapabilities)
            {
                if (capability.Equals("TargetNode"))
                {
                    isTargetNode = true;
                }
                else if (capability.StartsWith("$TFM:"))
                {
                    potentialTFM = capability.Substring("$TFM:".Length);
                }
            }

            return isTargetNode ? potentialTFM : null;
        }

        // This method is separate from CreateCollectionSource and marked with
        // MethodImplOptions.NoInlining because we don't want calls to CreateCollectionSource
        // to cause Roslyn assemblies to load where they aren't needed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IAttachedCollectionSource CreateCollectionSourceCore(IVsHierarchyItem projectRootItem, IVsHierarchyItem item, string targetFrameworkMoniker)
        {
            var hierarchyMapper = TryGetProjectMap();
            if (hierarchyMapper != null &&
                hierarchyMapper.TryGetProjectId(projectRootItem, targetFrameworkMoniker, out var projectId))
            {
                var workspace = TryGetWorkspace();
                var analyzerService = GetAnalyzerService();

                var hierarchy = projectRootItem.HierarchyIdentity.NestedHierarchy;
                var itemId = projectRootItem.HierarchyIdentity.NestedItemID;
                if (hierarchy.GetCanonicalName(itemId, out var projectCanonicalName) == VSConstants.S_OK)
                {
                    return new CpsDiagnosticItemSource(workspace, projectCanonicalName, projectId, item, _commandHandler, analyzerService);
                }
            }

            return null;
        }

        private static bool NestedHierarchyHasProjectTreeCapability(IVsHierarchyItem item, string capability)
        {
            var hierarchy = item.HierarchyIdentity.NestedHierarchy;
            var itemId = item.HierarchyIdentity.NestedItemID;

            var projectTreeCapabilities = GetProjectTreeCapabilities(hierarchy, itemId);
            return projectTreeCapabilities.Any(c => c.Equals(capability));
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

        private IDiagnosticAnalyzerService GetAnalyzerService()
        {
            if (_diagnosticAnalyzerService == null)
            {
                _diagnosticAnalyzerService = _componentModel.GetService<IDiagnosticAnalyzerService>();
            }

            return _diagnosticAnalyzerService;
        }

    }
}
