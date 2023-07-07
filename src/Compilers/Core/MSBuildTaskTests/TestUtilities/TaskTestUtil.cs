// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;

internal static class TaskTestUtil
{
    public static void AssertCommandLine(
        ManagedToolTask task,
        params string[] expected)
    {
        var line = string.Join(' ', expected);
        Assert.Equal(line, task.GenerateResponseFileContents());

        if (RuntimeHostInfo.IsCoreClrRuntime)
        {
            line = $"{RuntimeHostInfo.GetDotNetPathOrDefault()} exec \"{task.PathToManagedTool}\" {line}";
        }

        Assert.Equal("/noconfig", task.GenerateCommandLineContents());
    }
}
