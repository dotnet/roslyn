// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Other
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpUpdateProjectToAllowUnsafe : AbstractIntegrationTest
    {
        public CSharpUpdateProjectToAllowUnsafe(VisualStudioInstanceFactory instanceFactory) : base(instanceFactory)
        {
        }

        private XElement InvokeFixAndGetProjectFileElement(ProjectUtils.Project project)
        {
            VisualStudio.SolutionExplorer.AddFile(project, "C.cs", @"
unsafe class C
{
}", open: true);

            VisualStudio.Editor.PlaceCaret("C");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Allow unsafe code in this project", applyFix: true);

            // Save the project file.
            VisualStudio.SolutionExplorer.SaveAll();

            var updatedProjectFile = VisualStudio.SolutionExplorer.GetFileContents(project, project.Name + ".csproj");
            return XElement.Parse(updatedProjectFile);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUpdateProjectToAllowUnsafe)]
        public void CPSProject_GeneralPropertyGroupUpdated()
        {
            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetStandardClassLibrary, LanguageNames.CSharp);

            Assert.True(InvokeFixAndGetProjectFileElement(project).Elements()
                .Where(e => e.Name.LocalName == "PropertyGroup" && !e.Attributes().Any(a => a.Name.LocalName == "Condition"))
                .Any(g => g.Elements().SingleOrDefault(e => e.Name.LocalName == "AllowUnsafeBlocks")?.Value == "true"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
        public void LegacyProject_AllConfigurationsUpdated()
        {
            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);

            Assert.True(InvokeFixAndGetProjectFileElement(project).Elements()
                .Where(e => e.Name.LocalName == "PropertyGroup" && e.Attributes().Any(a => a.Name.LocalName == "Condition"))
                .All(g => g.Elements().SingleOrDefault(e => e.Name.LocalName == "AllowUnsafeBlocks")?.Value == "true"));
        }

        [WorkItem(23342, "https://github.com/dotnet/roslyn/issues/23342")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUpdateProjectToAllowUnsafe)]
        public void LegacyProject_MultiplePlatforms_AllConfigurationsUpdated()
        {
            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.AddCustomProject(project, ".csproj", $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition=""'$(Configuration)' == ''"">Debug</Configuration>
    <Platform Condition=""'$(Platform)' == ''"">x64</Platform>
    <ProjectGuid>{{F4233BA4-A4CB-498B-BBC1-65A42206B1BA}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>{ProjectName}</RootNamespace>
    <AssemblyName>{ProjectName}</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x86'"">
    <OutputPath>bin\x86\Debug\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x86'"">
    <OutputPath>bin\x86\Release\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x64'"">
    <OutputPath>bin\x64\Debug\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
    <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x64'"">
    <OutputPath>bin\x64\Release\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>");

            Assert.True(InvokeFixAndGetProjectFileElement(project).Elements()
                .Where(e => e.Name.LocalName == "PropertyGroup" && e.Attributes().Any(a => a.Name.LocalName == "Condition"))
                .All(g => g.Elements().SingleOrDefault(e => e.Name.LocalName == "AllowUnsafeBlocks")?.Value == "true"));
        }
    }
}
