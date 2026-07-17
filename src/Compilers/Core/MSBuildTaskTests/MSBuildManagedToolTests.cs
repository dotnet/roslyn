// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using System;
#endif
using System.IO;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

// GetBuildTaskDirectory_UsesSdkRootAppContextData mutates the process-global "Microsoft.DotNet.Sdk.Root"
// AppContext value, which GetBuildTaskDirectory (and therefore GetToolDirectory) reads. A non-parallel
// collection gives this class exclusive execution so the mutation cannot leak into any other test.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ManagedToolTaskTestCollection
{
    public const string Name = nameof(ManagedToolTaskTestCollection);
}

[Collection(ManagedToolTaskTestCollection.Name)]
public sealed class MSBuildManagedToolTests
{
    [Fact]
    public void PathToBuiltinTool()
    {
        var taskPath = Path.GetDirectoryName(typeof(ManagedCompiler).Assembly.Location)!;
        var relativePath = RuntimeHostInfo.IsCoreClrRuntime
            ? Path.Combine("bincore", $"csc{PlatformInformation.ExeExtension}")
            : "csc.exe";
        var task = new Csc();
        Assert.Equal(Path.Combine(taskPath, relativePath), task.PathToBuiltInTool);
    }

    [Fact]
    public void IsSdkFrameworkToCoreBridgeTask()
    {
        Assert.False(ManagedToolTask.IsSdkFrameworkToCoreBridgeTask);
    }

    [Fact]
    public void IsBuiltinToolRunningOnCoreClr()
    {
        Assert.Equal(RuntimeHostInfo.IsCoreClrRuntime, ManagedToolTask.IsBuiltinToolRunningOnCoreClr);
    }

#if NET
    [Fact]
    public void GetBuildTaskDirectory_UsesSdkRootAppContextData()
    {
        // In the partially-AOT SDK CLI the host publishes the versioned SDK directory as this AppContext
        // value; GetBuildTaskDirectory reads it and appends the "Roslyn" subfolder (this branch is .NET only).
        // The value is not set in a normal test host - verify that first, then restore it in the finally.
        const string sdkRootDataName = "Microsoft.DotNet.Sdk.Root";
        Assert.Null(AppContext.GetData(sdkRootDataName));

        var sdkDirectory = Path.Combine(Path.GetTempPath(), nameof(GetBuildTaskDirectory_UsesSdkRootAppContextData));
        AppContext.SetData(sdkRootDataName, sdkDirectory);
        try
        {
            var expectedBuildTaskDirectory = Path.Combine(sdkDirectory, "Roslyn");
            var expectedToolDirectory = Path.Combine(expectedBuildTaskDirectory, "bincore");

            // Derive a task so the resolved folders can be observed end to end.
            var task = new TestManagedToolTask();

            Assert.Equal(expectedBuildTaskDirectory, ManagedToolTask.GetBuildTaskDirectory());
            Assert.Equal(expectedToolDirectory, ManagedToolTask.GetToolDirectory());
            Assert.Equal(Path.Combine(expectedToolDirectory, "testtool.dll"), task.PathToBuiltInTool);
        }
        finally
        {
            AppContext.SetData(sdkRootDataName, null);
        }

        Assert.Null(AppContext.GetData(sdkRootDataName));
    }

    private sealed class TestManagedToolTask : ManagedToolTask
    {
        public TestManagedToolTask()
            : base(ErrorString.ResourceManager)
        {
        }

        protected override string ToolNameWithoutExtension => "testtool";

        protected override void AddCommandLineCommands(CommandLineBuilderExtension commandLine)
        {
        }

        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
        }
    }
#endif
}
