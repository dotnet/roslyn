// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name("AnalyzerItemsProvider")]
    [Order]
    internal class AnalyzerItemProvider : AttachedCollectionSourceProvider<AnalyzersFolderItem>
    {
        protected override IAttachedCollectionSource CreateCollectionSource(AnalyzersFolderItem analyzersFolder, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                return new AnalyzerItemSource(analyzersFolder);
            }

            return null;
        }
    }
}
