// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name("GeneratedSourceFolderProvider")]
    [Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class GeneratedSourceFolderItemProvider : ProjectFolderItemProvider
    {
        [ImportingConstructor]
        public GeneratedSourceFolderItemProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) :
            base(serviceProvider)
        {
        }

        protected override IAttachedCollectionSource CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
        {
            // Check the parent is a top-level (project) item.
            if (item != null &&
                item.Parent != null &&
                item.Parent.Parent == null)
            {
                var projectId = TryGetProject(item);
                if (projectId != null)
                {
                    var workspace = TryGetWorkspace();
                    return new GeneratedSourceFolderItemSource(workspace, projectId, item);
                }
            }

            return null;
        }
    }
}
