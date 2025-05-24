// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking;

[ExportWorkspaceService(typeof(IPreviewDialogService), ServiceLayer.Test), Shared, PartNotDiscoverable]
internal sealed class MockPreviewDialogService : IPreviewDialogService, IWorkspaceServiceFactory
{
    public bool ReturnsNull;
    public bool Called;
    public string Title;
    public string HelpString;
    public string Description;
    public string TopLevelName;
    public Glyph TopLevelGlyph;
    public bool ShowCheckBoxes;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MockPreviewDialogService()
    {
    }

    public Solution PreviewChanges(string title, string helpString, string description, string topLevelName, Glyph topLevelGlyph, Solution newSolution, Solution oldSolution, bool showCheckBoxes = true)
    {
        Called = true;
        Title = title;
        HelpString = helpString;
        Description = description;
        TopLevelName = topLevelName;
        TopLevelGlyph = topLevelGlyph;
        ShowCheckBoxes = showCheckBoxes;

        return ReturnsNull ? null : newSolution;
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => this;
}
