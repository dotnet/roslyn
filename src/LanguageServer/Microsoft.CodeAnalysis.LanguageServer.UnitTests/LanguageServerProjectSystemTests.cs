// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectSystemTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task RegisteredProjectPathExcludesFileBasedApps()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectSystem = server.GetRequiredLspService<LanguageServerProjectSystem>();
        var directory = TempRoot.CreateDirectory();

        Assert.Equal(LanguageNames.CSharp, projectSystem.GetLanguageNameForRegisteredProjectPath(Path.Combine(directory.Path, "Project.csproj")));
        Assert.Null(projectSystem.GetLanguageNameForRegisteredProjectPath(Path.Combine(directory.Path, "Program.cs")));
    }
}
