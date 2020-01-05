// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
{
    [ExportWorkspaceService(typeof(IPreviewDialogService), "Test"), Shared]
    internal class MockPreviewDialogService : IPreviewDialogService, IWorkspaceServiceFactory
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
        {
            return this;
        }
    }
}
