// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal partial class AnalyzersFolderItem
{
    private sealed class BrowseObject(AnalyzersFolderItem analyzersFolderItem) : LocalizableProperties
    {
        [Browsable(false)]
        public AnalyzersFolderItem Folder { get; } = analyzersFolderItem;

        public override string GetClassName()
            => SolutionExplorerShim.Folder_Properties;

        public override string GetComponentName()
            => Folder.Text;
    }
}
