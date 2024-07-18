// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal partial class AnalyzerItem(
    AnalyzersFolderItem analyzersFolder,
    AnalyzerReference analyzerReference,
    IContextMenuController contextMenuController) : BaseItem(GetNameText(analyzerReference))
{
    private readonly AnalyzersFolderItem _analyzersFolder = analyzersFolder;
    private readonly IContextMenuController _contextMenuController = contextMenuController;

    public AnalyzerReference AnalyzerReference { get; } = analyzerReference;

    public override ImageMoniker IconMoniker
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
            if (this.AnalyzerReference is UnresolvedAnalyzerReference)
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
        _analyzersFolder.RemoveAnalyzer(this.AnalyzerReference.FullPath);
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
