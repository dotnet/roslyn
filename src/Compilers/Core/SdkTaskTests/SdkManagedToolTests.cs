// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit.Abstractions;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.Sdk.UnitTests;

public sealed class SdkManagedToolTests
{
    public ITestOutputHelper TestOutputHelper { get; }

    public SdkManagedToolTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    [Theory, CombinatorialData]
    public void PathToBuiltinTool(bool useDotNetHost)
    {
        var taskPath = Path.GetDirectoryName(typeof(ManagedCompiler).Assembly.Location)!;
        var task = new Csc { UseDotNetHost = useDotNetHost };
        var ext = useDotNetHost ? "dll" : "exe";
        Assert.Equal(Path.Combine(taskPath, "..", "bincore", $"csc.{ext}"), task.PathToBuiltInTool);
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
