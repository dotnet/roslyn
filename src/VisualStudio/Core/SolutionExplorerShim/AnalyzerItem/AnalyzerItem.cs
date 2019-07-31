// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class AnalyzerItem : BaseItem
    {
        private readonly AnalyzersFolderItem _analyzersFolder;
        private readonly AnalyzerReference _analyzerReference;
        private readonly IContextMenuController _contextMenuController;

        public AnalyzerItem(AnalyzersFolderItem analyzersFolder, AnalyzerReference analyzerReference, IContextMenuController contextMenuController)
            : base(GetNameText(analyzerReference))
        {
            _analyzersFolder = analyzersFolder;
            _analyzerReference = analyzerReference;
            _contextMenuController = contextMenuController;
        }

        public override ImageMoniker IconMoniker
        {
            get
            {
                return KnownMonikers.CodeInformation;
            }
        }

        public override ImageMoniker ExpandedIconMoniker
        {
            get
            {
                return KnownMonikers.CodeInformation;
            }
        }

        public override ImageMoniker OverlayIconMoniker
        {
            get
            {
                if (_analyzerReference is UnresolvedAnalyzerReference)
                {
                    return KnownMonikers.OverlayWarning;
                }
                else
                {
                    return default;
                }
            }
        }

        public override object GetBrowseObject()
        {
            return new BrowseObject(this);
        }

        public AnalyzerReference AnalyzerReference
        {
            get { return _analyzerReference; }
        }

        public override IContextMenuController ContextMenuController
        {
            get { return _contextMenuController; }
        }

        public AnalyzersFolderItem AnalyzersFolder
        {
            get { return _analyzersFolder; }
        }

        /// <summary>
        /// Remove this AnalyzerItem from it's folder.
        /// </summary>
        public void Remove()
        {
            _analyzersFolder.RemoveAnalyzer(_analyzerReference.FullPath);
        }

        private static string GetNameText(AnalyzerReference analyzerReference)
        {
            if (analyzerReference is UnresolvedAnalyzerReference)
            {
                return analyzerReference.FullPath;
            }
            else
            {
                return analyzerReference.Display;
            }
        }
    }
}
