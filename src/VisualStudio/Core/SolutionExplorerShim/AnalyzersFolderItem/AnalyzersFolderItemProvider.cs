// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name("AnalyzersFolderProvider")]
    [Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class AnalyzersFolderItemProvider : ProjectFolderItemProvider
    {
        private readonly IAnalyzersCommandHandler _commandHandler;

        [ImportingConstructor]
        public AnalyzersFolderItemProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler) :
            base(serviceProvider)
        {
            _commandHandler = commandHandler;
        }

        /// <summary>
        /// Constructor for use only in unit tests. Bypasses MEF to set the project mapper, workspace and glyph service.
        /// </summary>
        internal AnalyzersFolderItemProvider(IHierarchyItemToProjectIdMap projectMap, Workspace workspace, IAnalyzersCommandHandler commandHandler) :
            base(projectMap, workspace)
        {
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
                    var projectId = TryGetProject(item.Parent);
                    if (projectId != null)
                    {
                        var workspace = TryGetWorkspace();
                        return new AnalyzersFolderItemSource(workspace, projectId, item, _commandHandler);
                    }
                }
            }

            return null;
        }

        private static ImmutableArray<string> GetProjectTreeCapabilities(IVsHierarchy hierarchy, uint itemId)
        {
            object capabilitiesObj;
            if (hierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out capabilitiesObj) == VSConstants.S_OK)
            {
                var capabilitiesString = (string)capabilitiesObj;
                return ImmutableArray.Create(capabilitiesString.Split(' '));
            }
            else
            {
                return ImmutableArray<string>.Empty;
            }
        }
    }
}
