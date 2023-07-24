// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
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

#if NETCOREAPP
        Assert.Equal($"exec \"{task.PathToManagedTool}\"", task.GenerateCommandLineContents().Trim());

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

            var dotnetPath = RuntimeHostInfo.GetDotNetPathOrDefault();
            var expectedCommandLine = $@"{dotnetPath} exec ""{task.PathToManagedTool}"" {line}";

            bool isOnlyFileName = Path.GetFileName(dotnetPath).Length == dotnetPath.Length;
            if (isOnlyFileName)
            {
                // When ToolTask.GenerateFullPathToTool() returns only a file name (not a path to a file), MSBuild's ToolTask
                // will search the %PATH% (see https://github.com/dotnet/msbuild/blob/5410bf323451e04e99e79bcffd158e6d8d378149/src/Utilities/ToolTask.cs#L494-L513)
                // and log the full path to the exe. In this case, only assert that the commandLine ends with the expected
                // command line, and ignore the full path at the beginning.
                Assert.EndsWith(expectedCommandLine, commandLine);
            }
            else
            {
                Assert.Equal(expectedCommandLine, commandLine);
            }

            compilerTask.NoConfig = true;
            Assert.Equal("/noconfig", compilerTask.GenerateToolArguments());
        }
#endif
    }
}
