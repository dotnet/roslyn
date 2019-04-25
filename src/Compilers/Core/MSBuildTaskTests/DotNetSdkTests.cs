// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class DotNetSdkTests : DotNetSdkTestBase
    {
        [ConditionalFact(typeof(DotNetSdkAvailable))]
        public void TestSourceLink()
        {
            var sourcePackageDir = Temp.CreateDirectory();
            // TODO: test escaping (https://github.com/dotnet/roslyn/issues/22835): .CreateDirectory("a=b, c");

            var libFile = sourcePackageDir.CreateFile("lib.cs").WriteAllText("class Lib { public void M() { } }");

            var root1 = Path.GetFullPath(ProjectDir.Path + "\\");
            var root2 = Path.GetFullPath(sourcePackageDir.Path + "\\");

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
      <SourceRoot Include=""{root1}sub1\"" SourceControl=""git"" NestedRoot=""sub1"" ContainingRoot=""{root1}"" SourceLinkUrl=""https://raw.githubusercontent.com/M1/*""/>
      <SourceRoot Include=""{root1}sub2\"" SourceControl=""git"" NestedRoot=""sub2"" ContainingRoot=""{root1}"" SourceLinkUrl=""https://raw.githubusercontent.com/M2/*""/>
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
                    $@"{root2}: /_1/",
                    $@"{root1}: /_/",
                    $@"{root1}sub1\: /_/sub1/",
                    $@"{root1}sub2\: /_/sub2/",
                    "true",
                    $@"{root2}=/_1/,{root1}=/_/,PreviousPathMap",
                    "true"
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "[/_1/]=[https://raw.githubusercontent.com/Source/Package/*]," +
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
                    $@"{root2}: {root2}",
                    $@"{root1}: {root1}",
                    $@"{root1}sub1\: {root1}sub1\",
                    $@"{root1}sub2\: {root1}sub2\",
                    @"",
                    $@""
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[{root2}]=[https://raw.githubusercontent.com/Source/Package/*]," +
                $@"[{root1}]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[{root1}sub1\]=[https://raw.githubusercontent.com/M1/*]," +
                $@"[{root1}sub2\]=[https://raw.githubusercontent.com/M2/*]",
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
                    $@"{root2}: {root2}",
                    $@"{root1}: {root1}",
                    $@"{root1}sub1\: {root1}sub1\",
                    $@"{root1}sub2\: {root1}sub2\",
                    @"",
                    $@""
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[{root2}]=[https://raw.githubusercontent.com/Source/Package/*]," +
                $@"[{root1}]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[{root1}sub1\]=[https://raw.githubusercontent.com/M1/*]," +
                $@"[{root1}sub2\]=[https://raw.githubusercontent.com/M2/*]",
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
                    $@"{root2}: {root2}",
                    $@"{root1}: {root1}",
                    $@"{root1}sub1\: {root1}sub1\",
                    $@"{root1}sub2\: {root1}sub2\",
                    @"false",
                    $@""
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[{root2}]=[https://raw.githubusercontent.com/Source/Package/*]," +
                $@"[{root1}]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[{root1}sub1\]=[https://raw.githubusercontent.com/M1/*]," +
                $@"[{root1}sub2\]=[https://raw.githubusercontent.com/M2/*]",
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
                    $@"{root1}: /_/",
                    $@"{root2}: /_1/",
                    @"true",
                    $@"{root1}=/_/,{root2}=/_1/"
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[/_/]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[/_1/]=[https://raw.githubusercontent.com/Source/Package/*]",
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
                    $@"{root1}: /_/",
                    $@"{root2}: /_1/",
                    @"true",
                    $@"{root1}=/_/,{root2}=/_1/"
                });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"[/_/]=[https://raw.githubusercontent.com/R1/*]," +
                $@"[/_1/]=[https://raw.githubusercontent.com/Source/Package/*]",
                File.ReadAllText(sourceLinkJsonPath));
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
