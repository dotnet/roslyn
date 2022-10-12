// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
{
    [ExportWorkspaceService(typeof(IPreviewDialogService), ServiceLayer.Test), Shared, PartNotDiscoverable]
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockPreviewDialogService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => this;

        public Task<Solution> PreviewChangesAsync(
            string title,
            string helpString,
            string description,
            string? topLevelName,
            Glyph topLevelGlyph,
            Solution newSolution,
            Solution oldSolution,
            CancellationToken cancellationToken)
            => PreviewChangesAsync(title, helpString, description, topLevelName, topLevelGlyph, newSolution, oldSolution, showCheckBoxes: true, cancellationToken);

        public Task<Solution> PreviewChangesAsync(
            string title,
            string helpString,
            string description,
            string? topLevelName,
            Glyph topLevelGlyph,
            Solution newSolution,
            Solution oldSolution,
            bool showCheckBoxes,
            CancellationToken cancellationToken)
        {
            Called = true;
            Title = title;
            HelpString = helpString;
            Description = description;
            TopLevelName = topLevelName;
            TopLevelGlyph = topLevelGlyph;
            ShowCheckBoxes = showCheckBoxes;

            var result = ReturnsNull ? null : newSolution;
            return Task.FromResult(result);
        }
    }
}
