// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class WorkspaceFolderTrackerTests
{
    [Fact]
    public void UpdateUsesNormalizedCaseInsensitivePaths()
    {
        var workspaceFolderUri = new DocumentUri("file:///Workspace");
        var workspaceFolderPath = workspaceFolderUri.GetDocumentFilePathFromUri();
        var tracker = new WorkspaceFolderTracker();
        tracker.Initialize([new() { DocumentUri = workspaceFolderUri, Name = "Workspace" }]);

        tracker.Update([workspaceFolderPath.ToUpperInvariant()], []);

        Assert.Equal(Path.GetFullPath(workspaceFolderPath), Assert.Single(tracker.GetRequiredWorkspaceFolderPaths()));

        tracker.Update([], [workspaceFolderPath.ToUpperInvariant()]);

        Assert.Empty(tracker.GetRequiredWorkspaceFolderPaths());
    }
}
