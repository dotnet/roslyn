// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.Sdk.UnitTests;

public sealed class SdkManagedToolTests
{
    public ITestOutputHelper TestOutputHelper { get; }

    public SdkManagedToolTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    [Fact]
    public void PathToBuiltinTool()
    {
        var taskPath = Path.GetDirectoryName(typeof(ManagedCompiler).Assembly.Location)!;
        var task = new Csc();
        Assert.Contains(task.PathToBuiltInTool,
        new[]
        {
            Path.Combine(taskPath, "..", "bincore", "csc.dll"),
            Path.Combine(taskPath, "..", "bincore", "csc.exe"),
        });
    }

    [Fact]
    public void IsSdkFrameworkToCoreBridgeTask()
    {
        Assert.True(ManagedToolTask.IsSdkFrameworkToCoreBridgeTask);
    }

    [Fact]
    public void IsBuiltinToolRunningOnCoreClr()
    {
        Assert.True(ManagedToolTask.IsBuiltinToolRunningOnCoreClr);
    }
}
