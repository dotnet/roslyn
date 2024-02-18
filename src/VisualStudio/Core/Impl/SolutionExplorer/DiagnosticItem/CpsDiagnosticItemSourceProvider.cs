﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using OrderAttribute = Microsoft.VisualStudio.Utilities.OrderAttribute;
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(CpsDiagnosticItemSourceProvider))]
    [Order]
    [AppliesToProject($"({ProjectCapabilities.CSharp} | {ProjectCapabilities.VB}) & {ProjectCapabilities.Cps}")]
    internal sealed class CpsDiagnosticItemSourceProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
    {
        private readonly IAnalyzersCommandHandler _commandHandler;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly Workspace _workspace;

        private IHierarchyItemToProjectIdMap? _projectMap;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CpsDiagnosticItemSourceProvider(
            [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler,
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            VisualStudioWorkspace workspace)
        {
            _commandHandler = commandHandler;
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
            _workspace = workspace;
        }

        protected override IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem? item, string relationshipName)
        {
            if (item?.HierarchyIdentity?.NestedHierarchy != null &&
                !item.IsDisposed &&
                relationshipName == KnownRelationships.Contains)
            {
                if (NestedHierarchyHasProjectTreeCapability(item, "AnalyzerDependency"))
                {
                    var projectRootItem = FindProjectRootItem(item, out var targetFrameworkMoniker);
                    if (projectRootItem != null)
                    {
                        var hierarchyMapper = TryGetProjectMap();
                        if (hierarchyMapper != null &&
                            hierarchyMapper.TryGetProjectId(projectRootItem, targetFrameworkMoniker, out var projectId))
                        {
                            var hierarchy = projectRootItem.HierarchyIdentity.NestedHierarchy;
                            var itemId = projectRootItem.HierarchyIdentity.NestedItemID;
                            if (hierarchy.GetCanonicalName(itemId, out var projectCanonicalName) == VSConstants.S_OK)
                            {
                                return new CpsDiagnosticItemSource(_workspace, projectCanonicalName, projectId, item, _commandHandler, _diagnosticAnalyzerService);
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Starting at the given item, walks up the tree to find the item representing the project root.
        /// If the item is located under a target-framework specific node, the corresponding 
        /// TargetFrameworkMoniker will be found as well.
        /// </summary>
        private static IVsHierarchyItem? FindProjectRootItem(IVsHierarchyItem item, out string? targetFrameworkMoniker)
        {
            targetFrameworkMoniker = null;

            for (var parent = item; parent != null; parent = parent.Parent)
            {
                targetFrameworkMoniker ??= GetTargetFrameworkMoniker(parent);

                if (NestedHierarchyHasProjectTreeCapability(parent, "ProjectRoot"))
                {
                    return parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Given an item determines if it represents a particular target framework.
        /// If so, it returns the corresponding TargetFrameworkMoniker.
        /// </summary>
        private static string? GetTargetFrameworkMoniker(IVsHierarchyItem item)
        {
            if (TryGetFlags(item, out var flags) &&
                flags.Contains("TargetNode"))
            {
                const string prefix = "$TFM:";
                var flag = flags.FirstOrDefault(f => f.StartsWith(prefix));

                return flag?.Substring(prefix.Length);
            }

            return null;
        }

        private static bool NestedHierarchyHasProjectTreeCapability(IVsHierarchyItem item, string capability)
        {
            if (TryGetFlags(item, out var flags))
                return flags.Contains(capability);

            return false;
        }

        public static bool TryGetFlags(IVsHierarchyItem item, out ProjectTreeFlags flags)
        {
            if (item.HierarchyIdentity.IsRoot)
            {
                if (item.HierarchyIdentity.NestedHierarchy is IVsBrowseObjectContext { UnconfiguredProject.Services.ProjectTreeService.CurrentTree.Tree: { } tree })
                {
                    flags = tree.Flags;
                    return true;
                }
            }
            else
            {
                var itemId = item.HierarchyIdentity.ItemID;

                // Browse objects are created lazily, and we want to avoid creating them when possible.
                // This method is typically invoked for every hierarchy item in the tree, via Solution Explorer APIs.
                // Rather than create a browse object for every node, we find the project root node and use that.
                // In this way, we only ever create one browse object per project.
                var root = item;
                while (!root.HierarchyIdentity.IsRoot)
                {
                    root = root.Parent;
                }

                if (root.HierarchyIdentity.NestedHierarchy is IVsBrowseObjectContext { UnconfiguredProject.Services.ProjectTreeService.CurrentTree.Tree: { } tree })
                {
                    if (tree?.TryFind((IntPtr)itemId, out var subtree) == true)
                    {
                        flags = subtree.Flags;
                        return true;
                    }
                }
            }

            flags = default;
            return false;
        }

        private IHierarchyItemToProjectIdMap? TryGetProjectMap()
        {
            _projectMap ??= _workspace.Services.GetService<IHierarchyItemToProjectIdMap>();

            return _projectMap;
        }
    }
}
