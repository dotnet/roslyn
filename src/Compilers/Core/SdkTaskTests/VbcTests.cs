// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.Sdk.UnitTests;

public sealed class VbcTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib()
    {
        var vbc = new Vbc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.vb"),
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} /optionstrict:custom /out:test.exe test.vb", vbc.GenerateResponseFileContents());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_DisableSdkPath()
    {
        var vbc = new Vbc
        {
            Sources = MSBuildUtil.CreateTaskItems("test.vb"),
            DisableSdkPath = true,
        };

        AssertEx.Equal($"/sdkpath:{RuntimeEnvironment.GetRuntimeDirectory()} /optionstrict:custom /nosdkpath /out:test.exe test.vb", vbc.GenerateResponseFileContents());
    }
}
