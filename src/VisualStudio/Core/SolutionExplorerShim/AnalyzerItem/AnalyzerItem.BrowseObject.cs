// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class AnalyzerItem
    {
        internal class BrowseObject : LocalizableProperties
        {
            private AnalyzerItem _analyzerItem;

            public BrowseObject(AnalyzerItem analyzerItem)
            {
                _analyzerItem = analyzerItem;
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.AnalyzerItemNameDisplayName))]
            public string Name
            {
                get
                {
                    return _analyzerItem.AnalyzerReference.Display;
                }
            }

            [BrowseObjectDisplayName(nameof(SolutionExplorerShim.AnalyzerItemPathDisplayName))]
            public string Path
            {
                get
                {
                    return _analyzerItem.AnalyzerReference.FullPath;
                }
            }

            public override string GetClassName()
            {
                return SolutionExplorerShim.AnalyzerItem_PropertyWindowClassName;
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
