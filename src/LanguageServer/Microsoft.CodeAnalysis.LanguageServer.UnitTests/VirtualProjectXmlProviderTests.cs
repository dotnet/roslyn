// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Goal of these tests:
/// - Ensure that the various request/response forms work as expected in basic scenarios.
/// - Ensure that various properties on the response are populated in a reasonable way.
/// Non-goals:
/// - Thorough behavioral testing.
/// - Testing of more intricate behaviors which are subject to change.
/// </summary>
public sealed class VirtualProjectXmlProviderTests : AbstractLanguageServerHostTests
{
    public VirtualProjectXmlProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    private async Task<VirtualProjectXmlProvider> GetProjectXmlProviderAsync()
    {
        var (exportProvider, assemblyLoader) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, extensionPaths: null);

        return exportProvider.GetExportedValue<VirtualProjectXmlProvider>();
    }

    [Fact]
    public async Task GetProjectXml_FileBasedProgram_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();
        var appFile = tempDir.CreateFile("app.cs");
        await appFile.WriteAllTextAsync("""
            Console.WriteLine("Hello, world!");
            """);

        var globalJsonFile = tempDir.CreateFile("global.json");
        // TODO: these tests should be conditioned on availability of an SDK which supports run-api
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var contentNullable = await projectProvider.MakeVirtualProjectContentNewAsync(appFile.Path, CancellationToken.None);
        var content = contentNullable.Value;
        var virtualProjectXml = content.VirtualProjectXml;
        LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>().LogTrace(virtualProjectXml);

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", virtualProjectXml);
        Assert.Contains("<ArtifactsPath>", virtualProjectXml);
        Assert.Empty(content.Diagnostics);
    }

    [Fact]
    public async Task GetProjectXml_NonFileBasedProgram_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();
        var appFile = tempDir.CreateFile("app.cs");
        await appFile.WriteAllTextAsync("""
            public class C
            {
            }
            """);

        var globalJsonFile = tempDir.CreateFile("global.json");
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var contentNullable = await projectProvider.MakeVirtualProjectContentNewAsync(appFile.Path, CancellationToken.None);
        var content = contentNullable.Value;
        LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>().LogTrace(content.VirtualProjectXml);

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", content.VirtualProjectXml);
        Assert.Contains("<ArtifactsPath>", content.VirtualProjectXml);
        Assert.Empty(content.Diagnostics);
    }

    [Fact]
    public async Task GetProjectXml_BadPath_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();

        var globalJsonFile = tempDir.CreateFile("global.json");
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var content = await projectProvider.MakeVirtualProjectContentNewAsync(Path.Combine(tempDir.Path, "BAD"), CancellationToken.None);
        Assert.Null(content);
    }

    [Fact]
    public async Task GetProjectXml_BadDirective_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();
        var appFile = tempDir.CreateFile("app.cs");
        await appFile.WriteAllTextAsync("""
            #:package Newtonsoft.Json@13.0.3
            #:BAD
            Console.WriteLine("Hello, world!");
            """);

        var globalJsonFile = tempDir.CreateFile("global.json");
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var contentNullable = await projectProvider.MakeVirtualProjectContentNewAsync(appFile.Path, CancellationToken.None);
        var content = contentNullable.Value;
        var diagnostic = content.Diagnostics.Single();
        Assert.Contains("Unrecognized directive 'BAD'", diagnostic.Message);
        Assert.Equal(appFile.Path, diagnostic.Location.Path);

        // TODO: it seems wrong that the specific location of the bad directive is not reported.
        Assert.Equal("(0,0)-(0,0)", diagnostic.Location.Span.ToString());
    }
}
