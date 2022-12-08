// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class AnalyzerItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            private readonly AnalyzerItem _analyzerItem;

            public BrowseObject(AnalyzerItem analyzerItem)
            {
                _analyzerItem = analyzerItem;
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Name))]
            public string Name
            {
                get
                {
                    return _analyzerItem.AnalyzerReference.Display;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.Path))]
            public string Path
            {
                get
                {
                    return _analyzerItem.AnalyzerReference.FullPath;
                }
            }

            public override string GetClassName()
            {
                return SolutionExplorerShim.Analyzer_Properties;
            }

            public override string GetComponentName()
            {
                return _analyzerItem.Text;
            }

            [Browsable(false)]
            public AnalyzerItem AnalyzerItem
            {
                get { return _analyzerItem; }
            }
        }
    }
}
