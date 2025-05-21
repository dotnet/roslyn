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
        // TODO: we need a way to automatically obtain the needed SDK for running the test.
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var content = await projectProvider.MakeVirtualProjectContentNewAsync(appFile.Path, CancellationToken.None);
        Assert.NotNull(content);
        LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>().LogTrace(content.Value.VirtualProjectXml);

        // TODO: it's not clear what should be asserted about the resulting value.
        // Perhaps 'Contains("<ArtifactsPath>)' and 'Contains("<TargetFramework>...</TargetFramework>")').
        var expected = """
            <Project>

              <PropertyGroup>
                <IncludeProjectNameInArtifactsPaths>false</IncludeProjectNameInArtifactsPaths>
                <ArtifactsPath>C:\Users\rigibson\AppData\Local\Temp\dotnet\runfile\app-e58ea5315882fd4f9402b620afffcb6b18abdce2e6b76c205ac0cfc8db48593d</ArtifactsPath>
              </PropertyGroup>

              <!-- We need to explicitly import Sdk props/targets so we can override the targets below. -->
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <PropertyGroup>
                <EnableDefaultItems>false</EnableDefaultItems>
              </PropertyGroup>

              <PropertyGroup>
                <Features>$(Features);FileBasedProgram</Features>
              </PropertyGroup>

              <ItemGroup>
                <Compile Include="C:\Users\rigibson\AppData\Local\Temp\RoslynTests\716acaad-3be2-48e4-9981-79a878f127ea\app.cs" />
              </ItemGroup>

              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />

              <!--
                Override targets which don't work with project files that are not present on disk.
                See https://github.com/NuGet/Home/issues/14148.
              -->

              <Target Name="_FilterRestoreGraphProjectInputItems"
                      DependsOnTargets="_LoadRestoreGraphEntryPoints"
                      Returns="@(FilteredRestoreGraphProjectInputItems)">
                <ItemGroup>
                  <FilteredRestoreGraphProjectInputItems Include="@(RestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>

              <Target Name="_GetAllRestoreProjectPathItems"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems"
                      Returns="@(_RestoreProjectPathItems)">
                <ItemGroup>
                  <_RestoreProjectPathItems Include="@(FilteredRestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>

              <Target Name="_GenerateRestoreGraph"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems;_GenerateRestoreGraphProjectEntry;_GenerateProjectRestoreGraph"
                      Returns="@(_RestoreGraphEntry)">
                <!-- Output from dependency _GenerateRestoreGraphProjectEntry and _GenerateProjectRestoreGraph -->
              </Target>

            </Project>
            """;
        Assert.Equal(expected, content.Value.VirtualProjectXml);
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
        // TODO: we need a way to automatically obtain the needed SDK for running the test.
        await globalJsonFile.WriteAllTextAsync("""
            {
              "sdk": {
                "version": "10.0.100-preview.5.25265.12"
              }
            }
            """);

        var content = (await projectProvider.MakeVirtualProjectContentNewAsync(appFile.Path, CancellationToken.None)).Value;
        LoggerFactory.CreateLogger<VirtualProjectXmlProviderTests>().LogTrace(content.VirtualProjectXml);

        var expected = """
            <Project>

              <PropertyGroup>
                <IncludeProjectNameInArtifactsPaths>false</IncludeProjectNameInArtifactsPaths>
                <ArtifactsPath>C:\Users\rigibson\AppData\Local\Temp\dotnet\runfile\app-__PLACEHOLDER__</ArtifactsPath>
              </PropertyGroup>

              <!-- We need to explicitly import Sdk props/targets so we can override the targets below. -->
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <PropertyGroup>
                <EnableDefaultItems>false</EnableDefaultItems>
              </PropertyGroup>

              <PropertyGroup>
                <Features>$(Features);FileBasedProgram</Features>
              </PropertyGroup>

              <ItemGroup>
                <Compile Include="C:\Users\rigibson\AppData\Local\Temp\RoslynTests\__PLACEHOLDER__\app.cs" />
              </ItemGroup>

              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />

              <!--
                Override targets which don't work with project files that are not present on disk.
                See https://github.com/NuGet/Home/issues/14148.
              -->

              <Target Name="_FilterRestoreGraphProjectInputItems"
                      DependsOnTargets="_LoadRestoreGraphEntryPoints"
                      Returns="@(FilteredRestoreGraphProjectInputItems)">
                <ItemGroup>
                  <FilteredRestoreGraphProjectInputItems Include="@(RestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>

              <Target Name="_GetAllRestoreProjectPathItems"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems"
                      Returns="@(_RestoreProjectPathItems)">
                <ItemGroup>
                  <_RestoreProjectPathItems Include="@(FilteredRestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>

              <Target Name="_GenerateRestoreGraph"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems;_GenerateRestoreGraphProjectEntry;_GenerateProjectRestoreGraph"
                      Returns="@(_RestoreGraphEntry)">
                <!-- Output from dependency _GenerateRestoreGraphProjectEntry and _GenerateProjectRestoreGraph -->
              </Target>

            </Project>
            """;
        Assert.Matches(Regex.Escape(expected).Replace(@"__PLACEHOLDER__", ".*"), content.VirtualProjectXml);
        Assert.Empty(content.Diagnostics);
    }

    [Fact]
    public async Task GetProjectXml_BadPath_01()
    {
        var projectProvider = await GetProjectXmlProviderAsync();

        var tempDir = TempRoot.CreateDirectory();

        var globalJsonFile = tempDir.CreateFile("global.json");
        // TODO: we need a way to automatically obtain the needed SDK for running the test.
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
        // TODO: we need a way to automatically obtain the needed SDK for running the test.
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
