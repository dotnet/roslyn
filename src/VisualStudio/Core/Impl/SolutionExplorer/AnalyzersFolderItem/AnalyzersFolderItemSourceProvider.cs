﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(AnalyzersFolderItemSourceProvider))]
    [Order(Before = HierarchyItemsProviderNames.Contains)]
    [AppliesToProject("(CSharp | VisualBasic) & !CPS")] // in the CPS case, the Analyzers folder is created by the project system
    internal class AnalyzersFolderItemSourceProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
    {
        private readonly IAnalyzersCommandHandler _commandHandler;
        private IHierarchyItemToProjectIdMap? _projectMap;
        private readonly Workspace _workspace;

        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        [ImportingConstructor]
        public AnalyzersFolderItemSourceProvider(
            VisualStudioWorkspace workspace,
            [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler)
        {
            _workspace = workspace;
            _commandHandler = commandHandler;
        }

        protected override IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
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
                    var hierarchyMapper = TryGetProjectMap();
                    if (hierarchyMapper != null &&
                        hierarchyMapper.TryGetProjectId(item.Parent, targetFrameworkMoniker: null, projectId: out var projectId))
                    {
                        return new AnalyzersFolderItemSource(_workspace, projectId, item, _commandHandler);
                    }

                    return null;
                }
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

        private IHierarchyItemToProjectIdMap? TryGetProjectMap()
        {
            if (_projectMap == null)
            {
                _projectMap = _workspace.Services.GetService<IHierarchyItemToProjectIdMap>();
            }

            return _projectMap;
        }
    }
}
