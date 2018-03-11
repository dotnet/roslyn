// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class AnalyzersFolderItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            private readonly AnalyzersFolderItem _analyzersFolderItem;

            public BrowseObject(AnalyzersFolderItem analyzersFolderItem)
            {
                _analyzersFolderItem = analyzersFolderItem;
            }

            public override string GetClassName()
            {
                return SolutionExplorerShim.Folder_Properties;
            }

            public override string GetComponentName()
            {
                return _analyzersFolderItem.Text;
            }

            [Browsable(false)]
            public AnalyzersFolderItem Folder
            {
                get { return _analyzersFolderItem; }
            }
        }
    }
}
