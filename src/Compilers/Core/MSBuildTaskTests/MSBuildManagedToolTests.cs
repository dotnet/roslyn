// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

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
}
