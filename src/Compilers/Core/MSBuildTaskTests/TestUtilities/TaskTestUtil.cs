// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;

internal static class TaskTestUtil
{
    public static void AssertCommandLine(
        ManagedToolTask task,
        MockEngine engine,
        params string[] expected)
    {
        var line = string.Join(" ", expected);
        var rsp = task.GenerateResponseFileContents();
        Assert.Equal(line, rsp);
        Assert.Equal(expected, task.GenerateCommandLineArgsTaskItems(rsp).Select(x => x.ItemSpec));

#if NET
        Assert.Empty(task.GenerateCommandLineContents().Trim());

        // Can only run the Execute path on .NET Core presently. The internal workings of ToolTask 
        // will fail if it can't find the tool exe and we don't have csc.exe, vbc.exe, etc ... 
        // deployed in the unit tests. The .NET exe though is available hence Execute will run
        if (task is ManagedCompiler compilerTask)
        {
            compilerTask.SkipCompilerExecution = true;
            compilerTask.ProvideCommandLineArgs = true;
            Assert.True(compilerTask.Execute());
            Assert.Equal(expected, compilerTask.CommandLineArgs!.Select(x => x.ItemSpec));

            var message = engine.BuildMessages.OfType<TaskCommandLineEventArgs>().Single();
            var commandLine = message.CommandLine.Replace("  ", " ").Trim();
            AssertEx.Equal($@"{task.PathToBuiltInTool} {line}", commandLine);

            compilerTask.NoConfig = true;
            Assert.Equal("/noconfig", compilerTask.GenerateToolArguments());
        }
#endif
    }
}
