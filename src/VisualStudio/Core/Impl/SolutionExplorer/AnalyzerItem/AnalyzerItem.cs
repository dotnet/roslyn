// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class AnalyzerItem(
    AnalyzersFolderItem analyzersFolder,
    AnalyzerReference analyzerReference,
    IContextMenuController contextMenuController)
    : BaseItem(GetNameText(analyzerReference))
{
    public AnalyzersFolderItem AnalyzersFolder { get; } = analyzersFolder;
    public AnalyzerReference AnalyzerReference { get; } = analyzerReference;
    public override IContextMenuController ContextMenuController { get; } = contextMenuController;

    public override ImageMoniker IconMoniker => KnownMonikers.CodeInformation;

    public override ImageMoniker OverlayIconMoniker
        => this.AnalyzerReference is UnresolvedAnalyzerReference
            ? KnownMonikers.OverlayWarning
            : default;

    public override object GetBrowseObject()
        => new BrowseObject(this);

    /// <summary>
    /// Remove this AnalyzerItem from it's folder.
    /// </summary>
    public void Remove()
        => this.AnalyzersFolder.RemoveAnalyzer(this.AnalyzerReference.FullPath);

    private static string GetNameText(AnalyzerReference analyzerReference)
        => analyzerReference is UnresolvedAnalyzerReference unresolvedAnalyzerReference
           ? unresolvedAnalyzerReference.FullPath
           : analyzerReference.Display;
}
