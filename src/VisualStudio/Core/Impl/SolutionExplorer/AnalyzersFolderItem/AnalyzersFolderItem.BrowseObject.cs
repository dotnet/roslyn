// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class AnalyzersFolderItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            public BrowseObject(AnalyzersFolderItem analyzersFolderItem)
            {
                Folder = analyzersFolderItem;
            }

            public override string GetClassName()
            {
                return SolutionExplorerShim.Folder_Properties;
            }

            public override string GetComponentName()
            {
                return Folder.Text;
            }

            [Browsable(false)]
            public AnalyzersFolderItem Folder { get; }
        }
    }
}
