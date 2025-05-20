// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class FileBasedProgramProjectProviderTests : AbstractLanguageServerHostTests
{
    public FileBasedProgramProjectProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    private async Task<FileBasedProgramProjectProvider> GetProjectProviderAsync()
    {
        var (exportProvider, assemblyLoader) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, extensionPaths: null);

        return exportProvider.GetExportedValue<FileBasedProgramProjectProvider>();
    }

    [Fact]
    public async Task Test1()
    {
        var projectProvider = await GetProjectProviderAsync();

        var tempDir = TempRoot.CreateDirectory();
        var appFile = tempDir.CreateFile("app.cs");
        await appFile.WriteAllTextAsync("""
            Console.WriteLine("Hello, world!");
            """);

        var globalJsonFile = tempDir.CreateFile("global.json");
        // TODO: we need a way to automatically obtain the needed SDK for running the test.
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var content = await projectProvider.MakeVirtualProjectContentNewAsync(appFile.Path, CancellationToken.None);
        // TODO: check the resulting value
        Assert.NotNull(content);
    }
}
