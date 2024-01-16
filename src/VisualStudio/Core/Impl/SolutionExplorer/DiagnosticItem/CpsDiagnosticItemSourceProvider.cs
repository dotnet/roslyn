// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(CpsDiagnosticItemSourceProvider))]
    [Order]
    [AppliesToProject("(CSharp | VB) & CPS")]
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

        protected override IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
        {
            if (item != null &&
                item.HierarchyIdentity != null &&
                item.HierarchyIdentity.NestedHierarchy != null &&
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
        /// If the item is located under a target-framwork specific node, the corresponding 
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
            var hierarchy = item.HierarchyIdentity.NestedHierarchy;
            var itemId = item.HierarchyIdentity.NestedItemID;

            var isTargetNode = false;
            string? potentialTFM = null;

            MatchProjectTreeCapabilities(hierarchy, itemId, isMatch, state: false);

            return isTargetNode ? potentialTFM : null;

            bool isMatch(ReadOnlyMemory<char> capability, bool _)
            {
                if (ReadOnlyMemoryOfCharComparer.Equals("TargetNode".AsSpan(), capability))
                {
                    isTargetNode = true;
                }
                else if (capability.Span.StartsWith("$TFM:".AsSpan()))
                {
                    potentialTFM = capability["$TFM:".Length..].ToString();
                }

                return false;
            }
        }

        private static bool NestedHierarchyHasProjectTreeCapability(IVsHierarchyItem item, string capability)
        {
            var hierarchy = item.HierarchyIdentity.NestedHierarchy;
            var itemId = item.HierarchyIdentity.NestedItemID;

            return MatchProjectTreeCapabilities(hierarchy, itemId, isMatch, capability.AsMemory());

            static bool isMatch(ReadOnlyMemory<char> capability, ReadOnlyMemory<char> capabilityToMatch)
            {
                return ReadOnlyMemoryOfCharComparer.Equals(capabilityToMatch.Span, capability);
            }
        }

        private static bool MatchProjectTreeCapabilities<T>(IVsHierarchy hierarchy, uint itemId, Func<ReadOnlyMemory<char>, T, bool> isMatch, T state)
        {
            if (hierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out var capabilitiesObj) == VSConstants.S_OK)
            {
                var capabilitiesString = (string)capabilitiesObj;

                // Perf: Avoid string.Split
                var afterLastMatchIndex = 0;
                var nextMatchIndex = capabilitiesString.IndexOf(' ', afterLastMatchIndex);
                while (nextMatchIndex >= 0)
                {
                    if (isMatch(capabilitiesString.AsMemory(afterLastMatchIndex, nextMatchIndex - afterLastMatchIndex), state))
                        return true;

                    afterLastMatchIndex = nextMatchIndex + 1;
                    nextMatchIndex = capabilitiesString.IndexOf(' ', afterLastMatchIndex);
                }

                if (isMatch(capabilitiesString.AsMemory(afterLastMatchIndex), state))
                    return true;
            }

            return false;
        }

        private IHierarchyItemToProjectIdMap? TryGetProjectMap()
        {
            _projectMap ??= _workspace.Services.GetService<IHierarchyItemToProjectIdMap>();

            return _projectMap;
        }
    }
}
