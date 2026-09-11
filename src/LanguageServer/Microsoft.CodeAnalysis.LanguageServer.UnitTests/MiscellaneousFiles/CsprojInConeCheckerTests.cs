// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.FileBasedPrograms;

public sealed class CsprojInConeCheckerTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void UsesCurrentWorkspaceFolders()
    {
        var initialWorkspace = _tempRoot.CreateDirectory();
        var projectWorkspace = _tempRoot.CreateDirectory();
        projectWorkspace.CreateFile("Project.csproj");
        var sourceFile = projectWorkspace.CreateDirectory("src").CreateFile("Program.cs");
        var initialFolder = CreateWorkspaceFolder(initialWorkspace.Path);
        var projectFolder = CreateWorkspaceFolder(projectWorkspace.Path);
        var tracker = new WorkspaceFolderTracker();
        tracker.Update([initialFolder], removedFolders: null);
        var checker = new CsprojInConeChecker(tracker);

        Assert.False(checker.IsContainedInCsprojCone(sourceFile.Path));

        tracker.Update([projectFolder], [initialFolder]);

        Assert.True(checker.IsContainedInCsprojCone(sourceFile.Path));
    }

    private static WorkspaceFolder CreateWorkspaceFolder(string path)
        => new()
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(path),
            Name = Path.GetFileName(path),
        };
}
