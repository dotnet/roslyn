// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name("GeneratedSourceItemsProvider")]
    [Order]
    internal sealed class GeneratedSourceItemProvider : AttachedCollectionSourceProvider<GeneratedSourceFolderItem>
    {
        protected override IAttachedCollectionSource CreateCollectionSource(GeneratedSourceFolderItem analyzersFolder, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                return new GeneratedSourceItemSource(analyzersFolder);
            }

            return null;
        }
    }
}
