// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// uncomment the below define to dump binlogs of each test
//#define DUMP_MSBUILD_BIN_LOG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Roslyn.Test.Utilities;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class TargetTests : TestBase
    {
        [Fact]
        public void GenerateEditorConfigShouldNotRunWhenNoPropertiesOrMetadata()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFileShouldRun", GetTestLoggers());
            var shouldRun = instance.GetPropertyValue("_GeneratedEditorConfigShouldRun");
            var hasItems = instance.GetPropertyValue("_GeneratedEditorConfigHasItems");

            Assert.True(runSuccess);
            Assert.NotEqual("true", shouldRun);
            Assert.NotEqual("true", hasItems);
        }

        [Fact]
        public void GenerateEditorConfigShouldRunWhenPropertiesRequested()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

    <ItemGroup>
        <CompilerVisibleProperty Include=""prop"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFileShouldRun", GetTestLoggers());
            var shouldRun = instance.GetPropertyValue("_GeneratedEditorConfigShouldRun");
            var hasItems = instance.GetPropertyValue("_GeneratedEditorConfigHasItems");

            Assert.True(runSuccess);
            Assert.Equal("true", shouldRun);
            Assert.NotEqual("true", hasItems);
        }

        [Fact]
        public void GenerateEditorConfigShouldRunWhenMetadataRequested()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

    <ItemGroup>
        <CompilerVisibleItemMetadata Include=""item"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFileShouldRun", GetTestLoggers());
            var shouldRun = instance.GetPropertyValue("_GeneratedEditorConfigShouldRun");
            var hasItems = instance.GetPropertyValue("_GeneratedEditorConfigHasItems");

            Assert.True(runSuccess);
            Assert.Equal("true", shouldRun);
            Assert.Equal("true", hasItems);
        }

        [Fact]
        public void GenerateEditorConfigShouldRunWhenPropertiesAndMetadata()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

    <ItemGroup>
        <CompilerVisibleProperty Include=""prop"" />
        <CompilerVisibleItemMetadata Include=""item"" MetadataName=""meta"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFileShouldRun", GetTestLoggers());
            var shouldRun = instance.GetPropertyValue("_GeneratedEditorConfigShouldRun");
            var hasItems = instance.GetPropertyValue("_GeneratedEditorConfigHasItems");

            Assert.True(runSuccess);
            Assert.Equal("true", shouldRun);
            Assert.Equal("true", hasItems);
        }

        [Fact]
        public void GenerateEditorConfigCanBeDisabled()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <PropertyGroup>
        <GenerateMSBuildEditorConfigFile>false</GenerateMSBuildEditorConfigFile>
    </PropertyGroup>
    <ItemGroup>
        <CompilerVisibleProperty Include=""prop"" />
        <CompilerVisibleItemMetadata Include=""item"" MetadataName=""meta"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFileShouldRun", GetTestLoggers());
            var shouldRun = instance.GetPropertyValue("_GeneratedEditorConfigShouldRun");
            var hasItems = instance.GetPropertyValue("_GeneratedEditorConfigHasItems");

            Assert.True(runSuccess);
            Assert.NotEqual("true", shouldRun);
            Assert.Equal("true", hasItems);
        }

        [Fact]
        public void GenerateEditorConfigCoreEvaluatesProperties()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <PropertyGroup>
        <ValueToGet>abc</ValueToGet>
    </PropertyGroup>
    <ItemGroup>
        <CompilerVisibleProperty Include=""ValueToGet"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigProperty");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigProperty", item.ItemType);
            Assert.Single(item.Metadata);

            var metadata = item.Metadata.Single();
            Assert.Equal("Value", metadata.Name);
            Assert.Equal("abc", metadata.EvaluatedValue);
        }

        [Fact]
        public void GenerateEditorConfigCoreEvaluatesDynamicProperties()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <PropertyGroup>
        <RealValue>def</RealValue>
        <ValueToGet>$(RealValue)</ValueToGet>
    </PropertyGroup>
    <ItemGroup>
        <CompilerVisibleProperty Include=""ValueToGet"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigProperty");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigProperty", item.ItemType);
            Assert.Single(item.Metadata);

            var metadata = item.Metadata.Single();
            Assert.Equal("Value", metadata.Name);
            Assert.Equal("def", metadata.EvaluatedValue);
        }

        [Fact]
        public void GenerateEditorConfigCoreHandlesMissingProperties()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <ItemGroup>
        <CompilerVisibleProperty Include=""ValueToGet"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigProperty");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigProperty", item.ItemType);
            Assert.Single(item.Metadata);

            var metadata = item.Metadata.Single();
            Assert.Equal("Value", metadata.Name);
            Assert.Equal("", metadata.EvaluatedValue);
        }

        [Fact]
        public void GenerateEditorConfigCoreEvaluatesMetadata()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <ItemGroup>
        <Compile Include=""file1.cs"" CustomMeta=""abc"" />
    </ItemGroup>
    <ItemGroup>
        <CompilerVisibleItemMetadata Include=""Compile"" MetadataName=""CustomMeta"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigMetadata");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigMetadata", item.ItemType);

            var itemType = item.Metadata.SingleOrDefault(m => m.Name == "ItemType");
            AssertEx.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            AssertEx.NotNull(metaName);
            Assert.Equal("CustomMeta", metaName.EvaluatedValue);

            var customMeta = item.Metadata.SingleOrDefault(m => m.Name == metaName.EvaluatedValue);
            AssertEx.NotNull(customMeta);
            Assert.Equal("abc", customMeta.EvaluatedValue);
        }

        [Fact]
        public void GenerateEditorConfigCoreEvaluatesDynamicMetadata()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <PropertyGroup>
        <DynamicValue>abc</DynamicValue>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include=""file1.cs"" CustomMeta=""$(DynamicValue)"" />
    </ItemGroup>
    <ItemGroup>
        <CompilerVisibleItemMetadata Include=""Compile"" MetadataName=""CustomMeta"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigMetadata");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigMetadata", item.ItemType);

            var itemType = item.Metadata.SingleOrDefault(m => m.Name == "ItemType");
            AssertEx.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            AssertEx.NotNull(metaName);
            Assert.Equal("CustomMeta", metaName.EvaluatedValue);

            var customMeta = item.Metadata.SingleOrDefault(m => m.Name == metaName.EvaluatedValue);
            AssertEx.NotNull(customMeta);
            Assert.Equal("abc", customMeta.EvaluatedValue);
        }

        [Fact]
        public void GenerateEditorConfigCoreHandlesMissingMetadata()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <ItemGroup>
        <Compile Include=""file1.cs"" />
    </ItemGroup>
    <ItemGroup>
        <CompilerVisibleItemMetadata Include=""Compile"" MetadataName=""CustomMeta"" />
        <CompilerVisibleItemMetadata Include=""Compile2"" MetadataName=""CustomMeta"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigMetadata");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigMetadata", item.ItemType);

            var itemType = item.Metadata.SingleOrDefault(m => m.Name == "ItemType");
            AssertEx.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            AssertEx.NotNull(metaName);
            Assert.Equal("CustomMeta", metaName.EvaluatedValue);
        }

        [Fact]
        public void GenerateEditorConfigCoreHandlesMalformedCompilerVisibleItemMetadata()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <ItemGroup>
        <Compile Include=""file1.cs"" />
    </ItemGroup>
    <ItemGroup>
        <CompilerVisibleItemMetadata Include=""Compile"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("_GeneratedEditorConfigMetadata");
            Assert.Single(items);

            var item = items.Single();
            Assert.Equal("_GeneratedEditorConfigMetadata", item.ItemType);

            var itemType = item.Metadata.SingleOrDefault(m => m.Name == "ItemType");
            AssertEx.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            AssertEx.NotNull(metaName);
            Assert.Equal("", metaName.EvaluatedValue);
        }

        [Theory]
        [InlineData(".NETFramework", "4.5", "7.3")]
        [InlineData(".NETFramework", "4.7.2", "7.3")]
        [InlineData(".NETFramework", "4.8", "7.3")]

        [InlineData(".NETCoreApp", "1.0", "7.3")]
        [InlineData(".NETCoreApp", "2.0", "7.3")]
        [InlineData(".NETCoreApp", "2.1", "7.3")]
        [InlineData(".NETCoreApp", "3.0", "8.0")]
        [InlineData(".NETCoreApp", "3.1", "8.0")]
        [InlineData(".NETCoreApp", "5.0", "9.0")]
        [InlineData(".NETCoreApp", "6.0", "10.0")]
        [InlineData(".NETCoreApp", "7.0", "11.0")]
        [InlineData(".NETCoreApp", "8.0", "12.0")]
        [InlineData(".NETCoreApp", "9.0", "13.0")]
        [InlineData(".NETCoreApp", "10.0", "14.0")]
        [InlineData(".NETCoreApp", "11.0", "14.0")] // update cap when 15.0 is released

        [InlineData(".NETStandard", "1.0", "7.3")]
        [InlineData(".NETStandard", "1.5", "7.3")]
        [InlineData(".NETStandard", "2.0", "7.3")]
        [InlineData(".NETStandard", "2.1", "8.0")]

        [InlineData("UnknownTFM", "0.0", "7.3")]
        [InlineData("UnknownTFM", "5.0", "7.3")]
        [InlineData("UnknownTFM", "6.0", "7.3")]
        public void LanguageVersionGivenTargetFramework(string tfi, string tfv, string expectedVersion)
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <TargetFrameworkIdentifier>{tfi}</TargetFrameworkIdentifier>
        <_TargetFrameworkVersionWithoutV>{tfv}</_TargetFrameworkVersionWithoutV>
    </PropertyGroup>
    <Import Project=""Microsoft.CSharp.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);
            instance.Build(GetTestLoggers());

            var langVersion = instance.GetPropertyValue("LangVersion");
            var maxLangVersion = instance.GetPropertyValue("_MaxSupportedLangVersion");

            Assert.Equal(expectedVersion, langVersion);
            Assert.Equal(expectedVersion, maxLangVersion);

            // This will fail whenever the current language version is updated.
            // Ensure you update the target files to select the correct CSharp version for the newest target framework
            // and add to the theory data above to cover it, before changing this version to make the test pass again.
            Assert.Equal(CSharp.LanguageVersion.CSharp14, CSharp.LanguageVersionFacts.CurrentVersion);
        }

        [Fact]
        public void ExplicitLangVersion()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <TargetFrameworkIdentifier>.NETCoreApp</TargetFrameworkIdentifier>
        <_TargetFrameworkVersionWithoutV>2.0</_TargetFrameworkVersionWithoutV>
        <LangVersion>55.0</LangVersion>
    </PropertyGroup>
    <Import Project=""Microsoft.CSharp.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);
            instance.Build(GetTestLoggers());

            var langVersion = instance.GetPropertyValue("LangVersion");
            var maxLangVersion = instance.GetPropertyValue("_MaxSupportedLangVersion");

            Assert.Equal("55.0", langVersion);
            Assert.Equal("7.3", maxLangVersion);
        }

        [Fact]
        public void MaxSupportedLangVersionIsReadable()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <TargetFrameworkIdentifier>.NETCoreApp</TargetFrameworkIdentifier>
        <_TargetFrameworkVersionWithoutV>2.0</_TargetFrameworkVersionWithoutV>
        <LangVersion>9.0</LangVersion>
    </PropertyGroup>
    <Import Project=""Microsoft.CSharp.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);
            instance.Build(GetTestLoggers());

            var langVersion = instance.GetPropertyValue("LangVersion");
            var maxLangVersion = instance.GetPropertyValue("_MaxSupportedLangVersion");
            var publicMaxLangVersion = instance.GetPropertyValue("MaxSupportedLangVersion");

            Assert.Equal("9.0", langVersion);
            Assert.Equal("7.3", maxLangVersion);
            Assert.Equal("7.3", publicMaxLangVersion);
        }

        [Fact]
        public void MaxSupportedLangVersionIsnotWriteable()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <TargetFrameworkIdentifier>.NETCoreApp</TargetFrameworkIdentifier>
        <_TargetFrameworkVersionWithoutV>2.0</_TargetFrameworkVersionWithoutV>
        <LangVersion>9.0</LangVersion> 
        <MaxSupportedLangVersion>9.0</MaxSupportedLangVersion>
    </PropertyGroup>
    <Import Project=""Microsoft.CSharp.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);
            instance.Build(GetTestLoggers());

            var langVersion = instance.GetPropertyValue("LangVersion");
            var maxLangVersion = instance.GetPropertyValue("_MaxSupportedLangVersion");
            var publicMaxLangVersion = instance.GetPropertyValue("MaxSupportedLangVersion");

            Assert.Equal("9.0", langVersion);
            Assert.Equal("7.3", maxLangVersion);
            Assert.Equal("7.3", publicMaxLangVersion);
        }

        [Fact]
        public void GenerateEditorConfigIsPassedToTheCompiler()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

    <ItemGroup>
        <CompilerVisibleProperty Include=""prop"" />
    </ItemGroup>
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "GenerateMSBuildEditorConfigFile", GetTestLoggers());
            Assert.True(runSuccess);

            var items = instance.GetItems("EditorConfigFiles");
            Assert.Single(items);
        }

        [Fact]
        public void AdditionalFilesAreAddedToNoneWhenCopied()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
    <ItemGroup>
        <AdditionalFiles Include=""file1.cs"" CopyToOutputDirectory=""Always"" />
        <AdditionalFiles Include=""file2.cs"" CopyToOutputDirectory=""PreserveNewest"" />
        <AdditionalFiles Include=""file3.cs"" CopyToOutputDirectory=""Never"" />
        <AdditionalFiles Include=""file4.cs"" CopyToOutputDirectory="""" />
        <AdditionalFiles Include=""file5.cs"" />
    </ItemGroup>
</Project>
"));
            var instance = CreateProjectInstance(xmlReader);
            bool runSuccess = instance.Build(target: "CopyAdditionalFiles", GetTestLoggers());
            Assert.True(runSuccess);

            var noneItems = instance.GetItems("None").ToArray();
            Assert.Equal(3, noneItems.Length);

            Assert.Equal("file1.cs", noneItems[0].EvaluatedInclude);
            Assert.Equal("Always", noneItems[0].GetMetadataValue("CopyToOutputDirectory"));

            Assert.Equal("file2.cs", noneItems[1].EvaluatedInclude);
            Assert.Equal("PreserveNewest", noneItems[1].GetMetadataValue("CopyToOutputDirectory"));

            Assert.Equal("file3.cs", noneItems[2].EvaluatedInclude);
            Assert.Equal("Never", noneItems[2].GetMetadataValue("CopyToOutputDirectory"));
        }

        [Fact]
        public void GeneratedFilesOutputPathHasDefaults()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);
            var emit = instance.GetPropertyValue("EmitCompilerGeneratedFiles");
            var dir = instance.GetPropertyValue("CompilerGeneratedFilesOutputPath");

            Assert.Equal("false", emit);
            Assert.Equal(string.Empty, dir);
        }

        [Fact]
        public void GeneratedFilesOutputPathDefaultsToIntermediateOutputPathWhenSet()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <IntermediateOutputPath>fallbackDirectory</IntermediateOutputPath>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    </PropertyGroup>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "CreateCompilerGeneratedFilesOutputPath", GetTestLoggers());
            Assert.True(runSuccess);

            var emit = instance.GetPropertyValue("EmitCompilerGeneratedFiles");
            var dir = instance.GetPropertyValue("CompilerGeneratedFilesOutputPath");

            Assert.Equal("true", emit);
            Assert.Equal("fallbackDirectory/generated", dir);
        }

        [Fact]
        public void GeneratedFilesOutputPathDefaultsIsEmptyWhenEmitDisable()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>
        <IntermediateOutputPath>fallbackDirectory</IntermediateOutputPath>
    </PropertyGroup>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);
            var emit = instance.GetPropertyValue("EmitCompilerGeneratedFiles");
            var dir = instance.GetPropertyValue("CompilerGeneratedFilesOutputPath");

            Assert.Equal("false", emit);
            Assert.Equal(string.Empty, dir);
        }

        [Theory]
        [InlineData(true, "generatedDirectory")]
        [InlineData(true, null)]
        [InlineData(false, "generatedDirectory")]
        [InlineData(false, null)]
        public void GeneratedFilesOutputPathCanBeSetAndSuppressed(bool emitGeneratedFiles, string? generatedFilesDir)
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <EmitCompilerGeneratedFiles>{(emitGeneratedFiles.ToString().ToLower())}</EmitCompilerGeneratedFiles>
        <CompilerGeneratedFilesOutputPath>{generatedFilesDir}</CompilerGeneratedFilesOutputPath>
        <IntermediateOutputPath>fallbackDirectory</IntermediateOutputPath>
    </PropertyGroup>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "CreateCompilerGeneratedFilesOutputPath", GetTestLoggers());
            Assert.True(runSuccess);

            var emit = instance.GetPropertyValue("EmitCompilerGeneratedFiles");
            var dir = instance.GetPropertyValue("CompilerGeneratedFilesOutputPath");

            Assert.Equal(emitGeneratedFiles.ToString().ToLower(), emit);
            if (emitGeneratedFiles)
            {
                string expectedDir = generatedFilesDir ?? "fallbackDirectory/generated";
                Assert.Equal(expectedDir, dir);
            }
            else
            {
                Assert.Equal(string.Empty, dir);
            }
        }

        [Theory, CombinatorialData]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void TestSkipAnalyzers(
            [CombinatorialValues(true, false, null)] bool? runAnalyzers,
            [CombinatorialValues(true, false, null)] bool? runAnalyzersDuringBuild)
        {
            var runAnalyzersPropertyGroupString = getPropertyGroup("RunAnalyzers", runAnalyzers);
            var runAnalyzersDuringBuildPropertyGroupString = getPropertyGroup("RunAnalyzersDuringBuild", runAnalyzersDuringBuild);

            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

{runAnalyzersPropertyGroupString}
{runAnalyzersDuringBuildPropertyGroupString}

</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "_ComputeSkipAnalyzers", GetTestLoggers());
            Assert.True(runSuccess);

            // Verify "RunAnalyzers" overrides "RunAnalyzersDuringBuild".
            // If neither properties are set, analyzers are enabled by default.
            var analyzersEnabled = runAnalyzers ?? runAnalyzersDuringBuild ?? true;

            var expectedSkipAnalyzersValue = !analyzersEnabled ? "true" : "";
            var actualSkipAnalyzersValue = instance.GetPropertyValue("_SkipAnalyzers");
            Assert.Equal(expectedSkipAnalyzersValue, actualSkipAnalyzersValue);
            return;

            static string getPropertyGroup(string propertyName, bool? propertyValue)
            {
                if (!propertyValue.HasValue)
                {
                    return string.Empty;
                }

                return $@"
    <PropertyGroup>
        <{propertyName}>{propertyValue.Value}</{propertyName}>
    </PropertyGroup>";
            }
        }

        [Theory, CombinatorialData]
        [WorkItem(1337109, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1337109")]
        public void TestImplicitlySkipAnalyzers(
            [CombinatorialValues(true, false, null)] bool? runAnalyzers,
            [CombinatorialValues(true, false, null)] bool? implicitBuild,
            [CombinatorialValues(true, false, null)] bool? treatWarningsAsErrors,
            [CombinatorialValues(true, false, null)] bool? optimizeImplicitBuild,
            [CombinatorialValues(true, null)] bool? sdkStyleProject)
        {
            var runAnalyzersPropertyGroupString = getPropertyGroup("RunAnalyzers", runAnalyzers);
            var implicitBuildPropertyGroupString = getPropertyGroup("IsImplicitlyTriggeredBuild", implicitBuild);
            var treatWarningsAsErrorsPropertyGroupString = getPropertyGroup("TreatWarningsAsErrors", treatWarningsAsErrors);
            var optimizeImplicitBuildPropertyGroupString = getPropertyGroup("OptimizeImplicitlyTriggeredBuild", optimizeImplicitBuild);
            var sdkStyleProjectPropertyGroupString = getPropertyGroup("UsingMicrosoftNETSdk", sdkStyleProject);

            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

{runAnalyzersPropertyGroupString}
{implicitBuildPropertyGroupString}
{treatWarningsAsErrorsPropertyGroupString}
{optimizeImplicitBuildPropertyGroupString}
{sdkStyleProjectPropertyGroupString}


</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            bool runSuccess = instance.Build(target: "_ComputeSkipAnalyzers", GetTestLoggers());
            Assert.True(runSuccess);

            var analyzersEnabled = runAnalyzers ?? true;
            var expectedImplicitlySkippedAnalyzers = analyzersEnabled &&
                implicitBuild == true &&
                sdkStyleProject == true &&
                optimizeImplicitBuild != false &&
                (treatWarningsAsErrors != true || optimizeImplicitBuild == true);
            var expectedImplicitlySkippedAnalyzersValue = expectedImplicitlySkippedAnalyzers ? "true" : "";
            var actualImplicitlySkippedAnalyzersValue = instance.GetPropertyValue("_ImplicitlySkipAnalyzers");
            Assert.Equal(expectedImplicitlySkippedAnalyzersValue, actualImplicitlySkippedAnalyzersValue);

            var expectedSkipAnalyzersValue = !analyzersEnabled || expectedImplicitlySkippedAnalyzers ? "true" : "";
            var actualSkipAnalyzersValue = instance.GetPropertyValue("_SkipAnalyzers");
            Assert.Equal(expectedSkipAnalyzersValue, actualSkipAnalyzersValue);

            var expectedFeaturesValue = expectedImplicitlySkippedAnalyzers ? "run-nullable-analysis=never;" : "";
            var actualFeaturesValue = instance.GetPropertyValue("Features");
            Assert.Equal(expectedFeaturesValue, actualFeaturesValue);
            return;

            static string getPropertyGroup(string propertyName, bool? propertyValue)
            {
                if (!propertyValue.HasValue)
                {
                    return string.Empty;
                }

                return $@"
    <PropertyGroup>
        <{propertyName}>{propertyValue.Value}</{propertyName}>
    </PropertyGroup>";
            }
        }

        [Theory, CombinatorialData]
        [WorkItem(1337109, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1337109")]
        public void TestLastBuildWithSkipAnalyzers(
            [CombinatorialValues(true, false, null)] bool? runAnalyzers,
            bool lastBuildWithSkipAnalyzersFileExists)
        {
            var runAnalyzersPropertyGroupString = getPropertyGroup("RunAnalyzers", runAnalyzers);
            var intermediatePathDir = Temp.CreateDirectory();
            var intermediatePath = intermediatePathDir + Path.DirectorySeparatorChar.ToString();

            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />

{runAnalyzersPropertyGroupString}

    <PropertyGroup>
        <IntermediateOutputPath>{intermediatePath}</IntermediateOutputPath>
    </PropertyGroup>

</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            var msbuildProjectFileName = instance.GetPropertyValue("MSBuildProjectFile");
            var expectedLastBuildWithSkipAnalyzers = intermediatePath + msbuildProjectFileName + ".BuildWithSkipAnalyzers";
            if (lastBuildWithSkipAnalyzersFileExists)
            {
                _ = intermediatePathDir.CreateFile(msbuildProjectFileName + ".BuildWithSkipAnalyzers");
            }

            bool runSuccess = instance.Build(target: "_ComputeSkipAnalyzers", GetTestLoggers());
            Assert.True(runSuccess);

            var actualLastBuildWithSkipAnalyzers = instance.GetPropertyValue("_LastBuildWithSkipAnalyzers");
            Assert.Equal(expectedLastBuildWithSkipAnalyzers, actualLastBuildWithSkipAnalyzers);

            var skipAnalyzers = !(runAnalyzers ?? true);
            var expectedCustomAdditionalCompileInput = lastBuildWithSkipAnalyzersFileExists && !skipAnalyzers;

            var items = instance.GetItems("CustomAdditionalCompileInputs");
            var expectedItemCount = expectedCustomAdditionalCompileInput ? 1 : 0;
            Assert.Equal(expectedItemCount, items.Count);
            if (expectedCustomAdditionalCompileInput)
            {
                Assert.Equal(expectedLastBuildWithSkipAnalyzers, items.Single().EvaluatedInclude);
            }

            var expectedUpToDateCheckInput = lastBuildWithSkipAnalyzersFileExists && !skipAnalyzers;
            items = instance.GetItems("UpToDateCheckInput");
            expectedItemCount = expectedUpToDateCheckInput ? 1 : 0;
            Assert.Equal(expectedItemCount, items.Count);
            if (expectedUpToDateCheckInput)
            {
                var item = items.Single();
                Assert.Equal(expectedLastBuildWithSkipAnalyzers, item.EvaluatedInclude);
                Assert.Equal("ImplicitBuild", item.GetMetadataValue("Kind"));
            }

            return;

            static string getPropertyGroup(string propertyName, bool? propertyValue)
            {
                if (!propertyValue.HasValue)
                {
                    return string.Empty;
                }

                return $@"
    <PropertyGroup>
        <{propertyName}>{propertyValue.Value}</{propertyName}>
    </PropertyGroup>";
            }
        }

        [Fact]
        public void ProjectCapabilityIsNotAddedWhenRoslynComponentIsUnspecified()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            var caps = instance.GetItems("ProjectCapability").Select(c => c.EvaluatedInclude);
            Assert.DoesNotContain("RoslynComponent", caps);
        }

        [Fact]
        public void ProjectCapabilityIsAddedWhenRoslynComponentSpecified()
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader($@"
<Project>
    <PropertyGroup>
        <IsRoslynComponent>true</IsRoslynComponent>
    </PropertyGroup>
    <Import Project=""Microsoft.Managed.Core.targets"" />
</Project>
"));

            var instance = CreateProjectInstance(xmlReader);

            var caps = instance.GetItems("ProjectCapability").Select(c => c.EvaluatedInclude);
            Assert.Contains("RoslynComponent", caps);
        }

        [Fact]
        public void CompilerApiVersionIsSet()
        {
            var assembly = typeof(TargetTests).Assembly;
            var path = Path.Combine(Path.GetDirectoryName(assembly.Location)!, "Microsoft.Managed.Core.targets");
            XmlReader xmlReader = XmlReader.Create(new StringReader($"""
<Project>
    <Import Project="{path}" />
</Project>
"""));

            var instance = CreateProjectInstance(xmlReader);

            var compilerApiVersionString = instance.GetPropertyValue("CompilerApiVersion");
            Assert.StartsWith("roslyn", compilerApiVersionString);

            var compilerApiVersion = Version.Parse(compilerApiVersionString.Substring("roslyn".Length));

            var expectedVersionString = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "CurrentCompilerApiVersion")
                .Value ?? string.Empty;
            var expectedVersion = Version.Parse(expectedVersionString);

            Assert.Equal(expectedVersion, compilerApiVersion);
        }

        private static ProjectInstance CreateProjectInstance(XmlReader reader)
        {
            Project proj = new Project(reader);

            // add a dummy prepare for build target
            proj.Xml.AddTarget("PrepareForBuild");

            // create a dummy WriteLinesToFile task
            addTask(proj, "WriteLinesToFile", new()
            {
                { "Lines", "System.String[]" },
                { "File", "System.String" },
                { "Overwrite", "System.Boolean" },
                { "WriteOnlyWhenDifferent", "System.Boolean" }
            });

            // dummy makeDir task
            addTask(proj, "MakeDir", new()
            {
                { "Directories", "System.String[]" }
            });

            // dummy Message task
            addTask(proj, "Message", new()
            {
                { "Text", "System.String" },
                { "Importance", "System.String" }
            });

            // create an instance and return it
            return proj.CreateProjectInstance();

            static void addTask(Project proj, string taskName, Dictionary<string, string> parameters)
            {
                var task = proj.Xml.AddUsingTask(taskName, string.Empty, Assembly.GetExecutingAssembly().FullName);
                task.TaskFactory = nameof(DummyTaskFactory);

                var taskParams = task.AddParameterGroup();
                foreach (var kvp in parameters)
                {
                    taskParams.AddParameter(kvp.Key, string.Empty, string.Empty, kvp.Value);
                }

            }
        }

        private static ILogger[] GetTestLoggers([CallerMemberName] string callerName = "")
        {
#if DUMP_MSBUILD_BIN_LOG
            return new ILogger[]
            {
                new Build.Logging.BinaryLogger()
                {
                    Parameters = callerName + ".binlog"
                }
            };
#else
            return Array.Empty<ILogger>();
#endif
        }
    }

    /// <summary>
    /// Task factory that creates empty tasks for testing
    /// </summary>
    /// <remarks>
    /// Replace any task with a dummy task by adding a <c>UsingTask</c>
    /// <code>
    /// <UsingTask TaskName="[TaskToReplace]" TaskFactory="DummyTaskFactory">
    ///     <ParameterGroup>
    ///         <Param1 ParameterType="[Type]" />
    ///     </ParameterGroup>
    /// </UsingTask>
    /// </code>
    /// 
    /// You can specify the parameters the task should have via a <c>ParameterGroup</c>
    /// These should match the task you are replacing.
    /// </remarks>
    public sealed class DummyTaskFactory : ITaskFactory
    {
        public string FactoryName { get => "DummyTaskFactory"; }

        public Type TaskType { get => typeof(DummyTaskFactory); }

        private TaskPropertyInfo[]? _props;

        public void CleanupTask(ITask task) { }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost) => new DummyTask();

        public TaskPropertyInfo[]? GetTaskParameters() => _props;

        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            _props = parameterGroup.Values.ToArray();
            return true;
        }

        private class DummyTask : IGeneratedTask
        {
            public IBuildEngine? BuildEngine { get; set; }

            public ITaskHost? HostObject { get; set; }

            public bool Execute() => true;

            public object GetPropertyValue(TaskPropertyInfo property) => null!;

            public void SetPropertyValue(TaskPropertyInfo property, object value) { }
        }
    }
}
