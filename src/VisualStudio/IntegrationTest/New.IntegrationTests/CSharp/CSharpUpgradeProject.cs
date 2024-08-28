// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
public class CSharpUpgradeProject : AbstractUpgradeProjectTest
{
    private async Task InvokeFixAsync(string version, CancellationToken cancellationToken)
    {
        await TestServices.Editor.SetTextAsync(@$"
#error version:{version}
", cancellationToken);
        await TestServices.Editor.ActivateAsync(cancellationToken);

        await TestServices.Editor.PlaceCaretAsync($"version:{version}", charsOffset: 0, cancellationToken);

        // Suspend file change notification during code action application, since spurious file change notifications
        // can cause silent failure to apply the code action if they occur within this block.
        await using var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(cancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync($"Upgrade this project to C# language version '{version}'", applyFix: true, cancellationToken: cancellationToken);
    }

    [IdeFact]
    public async Task CPSProject_GeneralPropertyGroupUpdated()
    {
        var project = ProjectName;

        await TestServices.SolutionExplorer.CreateSolutionAsync(SolutionName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectAsync(project, WellKnownProjectTemplates.CSharpNetStandardClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(project, HangMitigatingCancellationToken);

        await InvokeFixAsync(version: "preview", HangMitigatingCancellationToken);
        VerifyPropertyOutsideConfiguration(await GetProjectFileElementAsync(project, HangMitigatingCancellationToken), "LangVersion", "preview");
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63026")]
    public async Task LegacyProject_AllConfigurationsUpdated()
    {
        var project = ProjectName;

        await TestServices.SolutionExplorer.CreateSolutionAsync(SolutionName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddCustomProjectAsync(
            project,
            ".csproj",
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition=""'$(Configuration)' == ''"">Debug</Configuration>
    <Platform Condition=""'$(Platform)' == ''"">x64</Platform>
    <ProjectGuid>{{F4233BA4-A4CB-498B-BBC1-65A42206B1BA}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>{ProjectName}</RootNamespace>
    <AssemblyName>{ProjectName}</AssemblyName>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
    <LangVersion>7.0</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x86'"">
    <OutputPath>bin\x86\Debug\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x86'"">
    <OutputPath>bin\x86\Release\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x64'"">
    <OutputPath>bin\x64\Debug\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x64'"">
    <OutputPath>bin\x64\Release\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>",
            HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(project, "C.cs", open: true, cancellationToken: HangMitigatingCancellationToken);

        await InvokeFixAsync(version: "7.3", HangMitigatingCancellationToken);
        VerifyPropertyInEachConfiguration(await GetProjectFileElementAsync(project, HangMitigatingCancellationToken), "LangVersion", "7.3");
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63026")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23342")]
    public async Task LegacyProject_MultiplePlatforms_AllConfigurationsUpdated()
    {
        var project = ProjectName;

        await TestServices.SolutionExplorer.CreateSolutionAsync(SolutionName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddCustomProjectAsync(
            project,
            ".csproj",
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition=""'$(Configuration)' == ''"">Debug</Configuration>
    <Platform Condition=""'$(Platform)' == ''"">x64</Platform>
    <ProjectGuid>{{F4233BA4-A4CB-498B-BBC1-65A42206B1BA}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>{ProjectName}</RootNamespace>
    <AssemblyName>{ProjectName}</AssemblyName>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x86'"">
    <OutputPath>bin\x86\Debug\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
    <LangVersion>7.2</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x86'"">
    <OutputPath>bin\x86\Release\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
    <LangVersion>7.1</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x64'"">
    <OutputPath>bin\x64\Debug\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>7.0</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x64'"">
    <OutputPath>bin\x64\Release\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>7.1</LangVersion>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>",
            HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(project, "C.cs", open: true, cancellationToken: HangMitigatingCancellationToken);

        await InvokeFixAsync(version: "7.3", HangMitigatingCancellationToken);
        VerifyPropertyInEachConfiguration(await GetProjectFileElementAsync(project, HangMitigatingCancellationToken), "LangVersion", "7.3");
    }
}
