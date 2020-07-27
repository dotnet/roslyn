// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class DotNetSdkTests : DotNetSdkTestBase
    {
        [ConditionalFact(typeof(DotNetSdkAvailable), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/46304")]
        [WorkItem(22835, "https://github.com/dotnet/roslyn/issues/22835")]
        public void TestSourceLink()
        {
            var sourcePackageDir = Temp.CreateDirectory().CreateDirectory("a=b, c");
            var libFile = sourcePackageDir.CreateFile("lib.cs").WriteAllText("class Lib { public void M() { } }");

            var root1 = Path.GetFullPath(ProjectDir.Path + Path.DirectorySeparatorChar);
            var root2 = Path.GetFullPath(sourcePackageDir.Path + Path.DirectorySeparatorChar);
            var root3 = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            root3 ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            root3 += Path.DirectorySeparatorChar;

            var escapedRoot1 = root1.Replace(",", ",,").Replace("=", "==");
            var escapedRoot2 = root2.Replace(",", ",,").Replace("=", "==");
            var escapedRoot3 = root3.Replace(",", ",,").Replace("=", "==");

            var sourceLinkJsonPath = Path.Combine(ObjDir.Path, ProjectName + ".sourcelink.json");

            var sourcePackageProps = $@"
  <ItemGroup>
    <Compile Include=""{libFile.Path}"" Link=""Lib.cs"" />
    <SourceRoot Include=""{root2}"" SourceLinkUrl=""https://raw.githubusercontent.com/Source/Package/*""/>
  </ItemGroup>
";

            var sourceLinkPackageTargets = $@"
  <PropertyGroup>
    <SourceLink>{sourceLinkJsonPath}</SourceLink>
  </PropertyGroup>
  <Target Name=""_InitializeSourceControlProperties"" BeforeTargets=""InitializeSourceControlInformation"">
    <ItemGroup>
      <SourceRoot Include=""{root1}"" SourceControl=""git"" SourceLinkUrl=""https://raw.githubusercontent.com/R1/*""/>
      <SourceRoot Include=""{root1}sub1{Path.DirectorySeparatorChar}"" SourceControl=""git"" NestedRoot=""sub1"" ContainingRoot=""{root1}"" SourceLinkUrl=""https://raw.githubusercontent.com/M1/*""/>
      <SourceRoot Include=""{root1}sub2{Path.DirectorySeparatorChar}"" SourceControl=""git"" NestedRoot=""sub2"" ContainingRoot=""{root1}"" SourceLinkUrl=""https://raw.githubusercontent.com/M2/*""/>
    </ItemGroup>
  </Target>
  <Target Name=""_GenerateSourceLinkFile""
          DependsOnTargets=""InitializeSourceRootMappedPaths""
          BeforeTargets=""CoreCompile""
          Outputs=""$(SourceLink)"">

    <WriteLinesToFile File=""$(SourceLink)""
                      Lines=""@(SourceRoot->'[%(MappedPath)]=[%(SourceLinkUrl)]', ',')""
                      Overwrite=""true""
                      Encoding=""UTF-8"" />

    <ItemGroup>
      <FileWrites Include=""$(SourceLink)"" />
    </ItemGroup>
  </Target>
";

            // deterministic CI build:
            VerifyValues(
                customProps: $@"
<PropertyGroup>
  <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  <PathMap>PreviousPathMap</PathMap>
</PropertyGroup>
{sourcePackageProps}",
                customTargets: sourceLinkPackageTargets,
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(SourceRoot->'%(Identity): %(MappedPath)')",
                    "$(DeterministicSourcePaths)",
                    "$(PathMap)",
                    "$(SourceRootMappedPathsFeatureSupported)"
                },
                expectedResults: new[]
                {
                    $@"{root3}: /_1/",
                    $@"{root2}: /_2/",
                    $@"{root1}: /_/",
                    $@"{root1}sub1{Path.DirectorySeparatorChar}: /_/sub1/",
                    $@"{root1}sub2{Path.DirectorySeparatorChar}: /_/sub2/",
                    "true",
                    $@"{escapedRoot3}=/_1/,{escapedRoot2}=/_2/,{escapedRoot1}=/_/,PreviousPathMap",
                    "true"
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "[/_1/]=[]," +
                "[/_2/]=[https://raw.githubusercontent.com/Source/Package/*]," +
                "[/_/]=[https://raw.githubusercontent.com/R1/*]," +
                "[/_/sub1/]=[https://raw.githubusercontent.com/M1/*]," +
                "[/_/sub2/]=[https://raw.githubusercontent.com/M2/*]",
                File.ReadAllText(sourceLinkJsonPath));

            // non-deterministic CI build:
            VerifyValues(
                customProps: $@"
<PropertyGroup>
  <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  <Deterministic>false</Deterministic>
</PropertyGroup>
{sourcePackageProps}",
                customTargets: sourceLinkPackageTargets,
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(SourceRoot->'%(Identity): %(MappedPath)')",
                    "$(DeterministicSourcePaths)",
                    "$(PathMap)"
                },
                expectedResults: new[]
                {
                    $@"{root3}: {root3}",
                    $@"{root2}: {root2}",
                    $@"{root1}: {root1}",
                    $@"{root1}sub1{Path.DirectorySeparatorChar}: {root1}sub1{Path.DirectorySeparatorChar}",
                    $@"{root1}sub2{Path.DirectorySeparatorChar}: {root1}sub2{Path.DirectorySeparatorChar}",
                    @"",
                    $@""
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[{root3}]=[]," +
                $@"[{root2}]=[https://raw.githubusercontent.com/Source/Package/*]," +
                $@"[{root1}]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[{root1}sub1{Path.DirectorySeparatorChar}]=[https://raw.githubusercontent.com/M1/*]," +
                $@"[{root1}sub2{Path.DirectorySeparatorChar}]=[https://raw.githubusercontent.com/M2/*]",
                File.ReadAllText(sourceLinkJsonPath));

            // deterministic local build:
            VerifyValues(
                customProps: $@"
<PropertyGroup>
  <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
  <ContinuousIntegrationBuild>false</ContinuousIntegrationBuild>
</PropertyGroup>
{sourcePackageProps}",
                customTargets: sourceLinkPackageTargets,
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(SourceRoot->'%(Identity): %(MappedPath)')",
                    "$(DeterministicSourcePaths)",
                    "$(PathMap)"
                },
                expectedResults: new[]
                {
                    $@"{root3}: {root3}",
                    $@"{root2}: {root2}",
                    $@"{root1}: {root1}",
                    $@"{root1}sub1{Path.DirectorySeparatorChar}: {root1}sub1{Path.DirectorySeparatorChar}",
                    $@"{root1}sub2{Path.DirectorySeparatorChar}: {root1}sub2{Path.DirectorySeparatorChar}",
                    @"",
                    $@""
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[{root3}]=[]," +
                $@"[{root2}]=[https://raw.githubusercontent.com/Source/Package/*]," +
                $@"[{root1}]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[{root1}sub1{Path.DirectorySeparatorChar}]=[https://raw.githubusercontent.com/M1/*]," +
                $@"[{root1}sub2{Path.DirectorySeparatorChar}]=[https://raw.githubusercontent.com/M2/*]",
                File.ReadAllText(sourceLinkJsonPath));

            // DeterministicSourcePaths override:
            VerifyValues(
                customProps: $@"
<PropertyGroup>
  <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
  <DeterministicSourcePaths>false</DeterministicSourcePaths>
</PropertyGroup>
{sourcePackageProps}",
                customTargets: sourceLinkPackageTargets,
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(SourceRoot->'%(Identity): %(MappedPath)')",
                    "$(DeterministicSourcePaths)",
                    "$(PathMap)"
                },
                expectedResults: new[]
                {
                    $@"{root3}: {root3}",
                    $@"{root2}: {root2}",
                    $@"{root1}: {root1}",
                    $@"{root1}sub1{Path.DirectorySeparatorChar}: {root1}sub1{Path.DirectorySeparatorChar}",
                    $@"{root1}sub2{Path.DirectorySeparatorChar}: {root1}sub2{Path.DirectorySeparatorChar}",
                    @"false",
                    $@""
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[{root3}]=[]," +
                $@"[{root2}]=[https://raw.githubusercontent.com/Source/Package/*]," +
                $@"[{root1}]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[{root1}sub1{Path.DirectorySeparatorChar}]=[https://raw.githubusercontent.com/M1/*]," +
                $@"[{root1}sub2{Path.DirectorySeparatorChar}]=[https://raw.githubusercontent.com/M2/*]",
                File.ReadAllText(sourceLinkJsonPath));

            // SourceControlInformationFeatureSupported = false:
            VerifyValues(
                customProps: $@"
<PropertyGroup>
  <DeterministicSourcePaths>true</DeterministicSourcePaths>
</PropertyGroup>
<ItemGroup>
  <SourceRoot Include=""{root1}"" SourceLinkUrl=""https://raw.githubusercontent.com/R1/*"" />
</ItemGroup>
{sourcePackageProps}",
                customTargets: $@"
<PropertyGroup>
  <SourceControlInformationFeatureSupported>false</SourceControlInformationFeatureSupported>
</PropertyGroup>
{sourceLinkPackageTargets}",
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(SourceRoot->'%(Identity): %(MappedPath)')",
                    "$(DeterministicSourcePaths)",
                    "$(PathMap)"
                },
                expectedResults: new[]
                {
                    $@"{root3}: /_/",
                    $@"{root1}: /_1/",
                    $@"{root2}: /_2/",
                    @"true",
                    $@"{escapedRoot3}=/_/,{escapedRoot1}=/_1/,{escapedRoot2}=/_2/,"
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[/_/]=[]," +
                $@"[/_1/]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[/_2/]=[https://raw.githubusercontent.com/Source/Package/*]",
                File.ReadAllText(sourceLinkJsonPath));

            // No SourceLink package:
            VerifyValues(
                customProps: $@"
<PropertyGroup>
  <DeterministicSourcePaths>true</DeterministicSourcePaths>
</PropertyGroup>
<ItemGroup>
  <SourceRoot Include=""{root1}"" SourceLinkUrl=""https://raw.githubusercontent.com/R1/*"" />
</ItemGroup>
{sourcePackageProps}",
                customTargets: @"
<PropertyGroup>
  <SourceControlInformationFeatureSupported>true</SourceControlInformationFeatureSupported>
</PropertyGroup>
",
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(SourceRoot->'%(Identity): %(MappedPath)')",
                    "$(DeterministicSourcePaths)",
                    "$(PathMap)"
                },
                expectedResults: new[]
                {
                    $@"{root3}: /_/",
                    $@"{root1}: /_1/",
                    $@"{root2}: /_2/",
                    @"true",
                    $@"{escapedRoot3}=/_/,{escapedRoot1}=/_1/,{escapedRoot2}=/_2/,"
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[/_/]=[]," +
                $@"[/_1/]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[/_2/]=[https://raw.githubusercontent.com/Source/Package/*]",
                File.ReadAllText(sourceLinkJsonPath));
        }

        [ConditionalTheory(typeof(DotNetSdkAvailable))]
        [CombinatorialData]
        [WorkItem(43476, "https://github.com/dotnet/roslyn/issues/43476")]
        public void InitializeSourceRootMappedPathsReturnsSourceMap(bool deterministicSourcePaths)
        {
            ProjectDir.CreateFile("Project2.csproj").WriteAllText($@"
<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <DeterministicSourcePaths>{deterministicSourcePaths}</DeterministicSourcePaths>
  </PropertyGroup>
  <ItemGroup>
    <SourceRoot Include=""X\""/>
    <SourceRoot Include=""Y\"" ContainingRoot=""X\"" NestedRoot=""A""/>
    <SourceRoot Include=""Z\"" ContainingRoot=""X\"" NestedRoot=""B""/>
  </ItemGroup>
</Project>
");

            VerifyValues(
                customProps: $@"
<ItemGroup>
<ProjectReference Include=""Project2.csproj"" Targets=""InitializeSourceRootMappedPaths"" OutputItemType=""ReferencedProjectSourceRoots"" ReferenceOutputAssembly=""false"" />
</ItemGroup>
",
                customTargets: null,
                targets: new[]
                {
                    "ResolveProjectReferences;_BeforeVBCSCoreCompile"
                },
                expressions: new[]
                {
                    "@(ReferencedProjectSourceRoots)",
                },
                expectedResults: new[]
                {
                    $"X{Path.DirectorySeparatorChar}",
                    $"Y{Path.DirectorySeparatorChar}",
                    $"Z{Path.DirectorySeparatorChar}",
                });
        }

        /// <summary>
        /// Validates dependencies of _BeforeVBCSCoreCompile target. 
        /// </summary>
        [ConditionalFact(typeof(DotNetSdkAvailable))]
        public void BeforeVBCSCoreCompileDependencies()
        {
            VerifyValues(
                customProps: $@"
  <ItemGroup>
    <ReferencePath Include=""A"" />
  </ItemGroup>",
                customTargets: null,
                targets: new[]
                {
                    "_BeforeVBCSCoreCompile"
                },
                expressions: new[]
                {
                    "@(ReferencePathWithRefAssemblies)",
                },
                expectedResults: new[]
                {
                    "A",
                });
        }

        [ConditionalFact(typeof(DotNetSdkAvailable))]
        public void ClearEmbedInteropTypes()
        {
            VerifyValues(
                customProps: $@"
  <PropertyGroup>
    <TargetingClr2Framework>true</TargetingClr2Framework>
  </PropertyGroup>
  <ItemGroup>
    <ReferencePathWithRefAssemblies Include=""A"" EmbedInteropTypes=""false""/>
    <ReferencePathWithRefAssemblies Include=""B"" EmbedInteropTypes=""true""/>
  </ItemGroup>",
                customTargets: null,
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(ReferencePathWithRefAssemblies->'EmbedInteropTypes=`%(EmbedInteropTypes)`')",
                },
                expectedResults: new[]
                {
                    "EmbedInteropTypes=``",
                    "EmbedInteropTypes=``"
                });
        }

        [ConditionalFact(typeof(DotNetSdkAvailable), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/34688")]
        public void TestDiscoverEditorConfigFiles()
        {
            var srcFile = ProjectDir.CreateFile("lib1.cs").WriteAllText("class C { }");
            var subdir = ProjectDir.CreateDirectory("subdir");
            var srcFile2 = subdir.CreateFile("lib2.cs").WriteAllText("class D { }");
            var editorConfigFile2 = subdir.CreateFile(".editorconfig").WriteAllText(@"[*.cs]
some_prop = some_val");
            VerifyValues(
                customProps: null,
                customTargets: null,
                targets: new[]
                {
                    "CoreCompile"
                },
                expressions: new[]
                {
                    "@(EditorConfigFiles)"
                },
                expectedResults: new[]
                {
                    Path.Combine(ProjectDir.Path, ".editorconfig"),
                    editorConfigFile2.Path
                });
        }
    }
}
