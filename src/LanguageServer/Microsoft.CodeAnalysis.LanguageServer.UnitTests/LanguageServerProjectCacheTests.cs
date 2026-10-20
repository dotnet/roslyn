// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectCacheTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task CachedReferencesPreserveAliasesAndEmbedInteropTypes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var directory = TempRoot.CreateDirectory();
        var projectPath = directory.CreateFile("Cached.csproj").Path;
        directory.CreateFile("Cached.csproj.lscache").WriteAllText("""
            version=2

            [project]
            language=C#

            [commandLineArguments]
            /target:library

            [analyzerReferences]
            cached analyzer.dll

            ---

            [project]
            language=C#

            [sliceDimensions]
            TargetFramework=net10.0

            [metadataReferences]
            cached reference,interop.dll
             @aliases=global,Interop
             @embedInteropTypes
            regular reference.dll
            """);
        var projectSystem = server.GetRequiredLspService<LanguageServerProjectSystem>();
        var cachedProject = await projectSystem.GetTestAccessor().TryLoadProjectFromCacheAsync(projectPath, CancellationToken.None);
        Assert.NotNull(cachedProject);

        var projectInfo = Assert.Single(cachedProject.Value.Item1);
        Assert.Equal(projectPath, projectInfo.FilePath);
        Assert.Equal(LanguageNames.CSharp, projectInfo.Language);
        AssertEx.Equal(["/target:library"], projectInfo.CommandLineArgs);

        Assert.NotNull(projectInfo.MetadataReferences);
        Assert.Equal(2, projectInfo.MetadataReferences.Length);

        var interopReference = projectInfo.MetadataReferences.Single(reference => reference.EmbedInteropTypes);
        Assert.Equal(Path.Combine(directory.Path, "cached reference,interop.dll"), interopReference.Path);
        AssertEx.Equal(["global", "Interop"], interopReference.Aliases);

        var regularReference = projectInfo.MetadataReferences.Single(reference => !reference.EmbedInteropTypes);
        Assert.Equal(Path.Combine(directory.Path, "regular reference.dll"), regularReference.Path);
        Assert.Empty(regularReference.Aliases);

        Assert.NotNull(projectInfo.AnalyzerReferences);
        Assert.Equal(Path.Combine(directory.Path, "cached analyzer.dll"), Assert.Single(projectInfo.AnalyzerReferences));
    }
}
