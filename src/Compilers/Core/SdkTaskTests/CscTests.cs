// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.Sdk.UnitTests;

public sealed class CscTests
{
    private static string RspFilePath => Path.Combine(Path.GetDirectoryName(typeof(ManagedCompiler).Assembly.Location)!, "csc.rsp");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib()
    {
        var csc = new Csc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.cs"),
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} @{RspFilePath} /out:test.exe test.cs", csc.GenerateResponseFileContents());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_DisableSdkPath()
    {
        var csc = new Csc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.cs"),
            DisableSdkPath = true,
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} @{RspFilePath} /nosdkpath /out:test.exe test.cs", csc.GenerateResponseFileContents());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_NoConfig()
    {
        var csc = new Csc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.cs"),
            NoConfig = true,
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} /out:test.exe test.cs", csc.GenerateResponseFileContents());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_CustomRsp()
    {
        var csc = new Csc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.cs"),
            ResponseFiles = MSBuildUtil.CreateTaskItems("custom.rsp"),
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} @{RspFilePath} test.cs @custom.rsp", csc.GenerateResponseFileContents());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_NoConfigAndCustomRsp()
    {
        var csc = new Csc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.cs"),
            NoConfig = true,
            ResponseFiles = MSBuildUtil.CreateTaskItems("custom.rsp"),
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} test.cs @custom.rsp", csc.GenerateResponseFileContents());
    }
}
