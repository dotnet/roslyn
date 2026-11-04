// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectSystemTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task OpenProjectsDoesNotLoadUnsupportedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectPath = TempRoot.CreateDirectory().CreateFile("Unsupported.unsupportedproj").Path;

        await server.OpenProjectsAsync([projectPath], CancellationToken.None);

        var projectSystem = server.GetRequiredLspService<LanguageServerProjectSystem>();
        Assert.Empty(await projectSystem.GetTestAccessor().GetLoadedProjectPathsAsync());
        Assert.Empty(server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution.Projects);
    }
}
