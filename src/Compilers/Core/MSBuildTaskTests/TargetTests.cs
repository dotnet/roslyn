// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// uncomment the below define to dump binlogs of each test
// #define DUMP_MSBUILD_BIN_LOG


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class TargetTests
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
        <CompilerVisibleItemMetadata Include=""item"" Metadata=""meta"" />
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
        <CompilerVisibleItemMetadata Include=""item"" Metadata=""meta"" />
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
            Assert.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            Assert.NotNull(metaName);
            Assert.Equal("CustomMeta", metaName.EvaluatedValue);

            var customMeta = item.Metadata.SingleOrDefault(m => m.Name == metaName.EvaluatedValue);
            Assert.NotNull(customMeta);
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
            Assert.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            Assert.NotNull(metaName);
            Assert.Equal("CustomMeta", metaName.EvaluatedValue);

            var customMeta = item.Metadata.SingleOrDefault(m => m.Name == metaName.EvaluatedValue);
            Assert.NotNull(customMeta);
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
            Assert.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            Assert.NotNull(metaName);
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
            Assert.NotNull(itemType);
            Assert.Equal("Compile", itemType.EvaluatedValue);

            var metaName = item.Metadata.SingleOrDefault(m => m.Name == "MetadataName");
            Assert.NotNull(metaName);
            Assert.Equal("", metaName.EvaluatedValue);
        }

        private ProjectInstance CreateProjectInstance(XmlReader reader)
        {
            Project proj = new Project(reader);

            // add a dummy prepare for build target
            proj.Xml.AddTarget("PrepareForBuild");

            // create a dummy WriteLinesToFile task
            var usingTask = proj.Xml.AddUsingTask("WriteLinesToFile", string.Empty, Assembly.GetExecutingAssembly().FullName);
            usingTask.TaskFactory = nameof(DummyTaskFactory);

            var taskParams = usingTask.AddParameterGroup();
            taskParams.AddParameter("Lines", "", "", "System.String[]");
            taskParams.AddParameter("File", "", "", "System.String");
            taskParams.AddParameter("Overwrite", "", "", "System.Boolean");
            taskParams.AddParameter("WriteOnlyWhenDifferent", "", "", "System.Boolean");

            // create an instance and return it
            return proj.CreateProjectInstance();
        }

        private ILogger[] GetTestLoggers([CallerMemberName] string callerName = "")
        {
#if DUMP_MSBUILD_BIN_LOG
            return new ILogger[]
            {
                new BinaryLogger()
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
