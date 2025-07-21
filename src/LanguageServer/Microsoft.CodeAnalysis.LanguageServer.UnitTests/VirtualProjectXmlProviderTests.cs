// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Uncomment this to test run-api locally.
// Eventually when a new enough SDK is adopted in-repo we can remove this
//#define RoslynTestRunApi

using System.Text;
using Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;
using Microsoft.Extensions.Logging;
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

    private class EnableRunApiTests : ExecutionCondition
    {
        // https://github.com/dotnet/roslyn/issues/78879: Enable these tests unconditionally
        public override bool ShouldSkip =>
#if RoslynTestRunApi
            false;
#else
            true;
#endif

        public override string SkipReason => $"Compilation symbol 'RoslynTestRunApi' is not defined.";
    }

    private async Task<VirtualProjectXmlProvider> GetProjectXmlProviderAsync()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, extensionPaths: null);
        return exportProvider.GetExportedValue<VirtualProjectXmlProvider>();
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/79464")]
    public async Task GetProjectXml_FileBasedProgram_SdkTooOld_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();
        var appFile = tempDir.CreateFile("app.cs");
        await appFile.WriteAllTextAsync("""
            Console.WriteLine("Hello, world!");
            """);

        var globalJsonFile = tempDir.CreateFile("global.json");
        globalJsonFile.WriteAllBytes(Encoding.UTF8.GetBytes("""
            {
              "sdk": {
                "version": "9.0.105"
              }
            }
            """));

        var contentNullable = await projectProvider.GetVirtualProjectContentAsync(appFile.Path, LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>(), CancellationToken.None);
        Assert.Null(contentNullable);
    }

    [ConditionalFact(typeof(EnableRunApiTests))]
    public async Task GetProjectXml_FileBasedProgram_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();
        var appFile = tempDir.CreateFile("app.cs");
        await appFile.WriteAllTextAsync("""
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

        var logger = LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>();
        var contentNullable = await projectProvider.GetVirtualProjectContentAsync(appFile.Path, logger, CancellationToken.None);
        var content = contentNullable.Value;
        var virtualProjectXml = content.VirtualProjectXml;
        logger.LogTrace(virtualProjectXml);

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", virtualProjectXml);
        Assert.Contains("<ArtifactsPath>", virtualProjectXml);
        Assert.Empty(content.Diagnostics);
    }

    [ConditionalFact(typeof(EnableRunApiTests))]
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

        var contentNullable = await projectProvider.GetVirtualProjectContentAsync(appFile.Path, LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>(), CancellationToken.None);
        var content = contentNullable.Value;
        LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>().LogTrace(content.VirtualProjectXml);

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", content.VirtualProjectXml);
        Assert.Contains("<ArtifactsPath>", content.VirtualProjectXml);
        Assert.Empty(content.Diagnostics);
    }

    [ConditionalFact(typeof(EnableRunApiTests))]
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

        var content = await projectProvider.GetVirtualProjectContentAsync(Path.Combine(tempDir.Path, "BAD"), LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>(), CancellationToken.None);
        Assert.Null(content);
    }

    [ConditionalFact(typeof(EnableRunApiTests))]
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

        var contentNullable = await projectProvider.GetVirtualProjectContentAsync(appFile.Path, LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>(), CancellationToken.None);
        var content = contentNullable.Value;
        var diagnostic = content.Diagnostics.Single();
        Assert.Contains("Unrecognized directive 'BAD'", diagnostic.Message);
        Assert.Equal(appFile.Path, diagnostic.Location.Path);

        // LinePositionSpan is not deserializing properly.
        // Address when implementing editor squiggles. https://github.com/dotnet/roslyn/issues/78688
        Assert.Equal("(0,0)-(0,0)", diagnostic.Location.Span.ToString());
    }
}
