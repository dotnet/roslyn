// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CommandLine;
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

    public static void AssertCompilerServerLogging(
        ManagedCompiler compilerTask,
        MockEngine engine,
        params string[] expectedArguments)
    {
        AssertCommandLine(compilerTask, engine, expectedArguments);

        compilerTask.SkipCompilerExecution = false;
        compilerTask.UseSharedCompilation = true;

        var messages = new List<(CompilerServerLogKind Kind, string Message)>();
        var innerLogger = new TestableCompilerServerLogger
        {
            LogFunc = (kind, message) => messages.Add((kind, message)),
        };
        var logger = new TaskCompilerServerLogger(new Microsoft.Build.Utilities.TaskLoggingHelper(compilerTask), innerLogger);
        var responseFileCommands = compilerTask.GenerateResponseFileContents();
        var commandLineCommands = compilerTask.GenerateCommandLineContents();

        var exitCode = compilerTask.ExecuteTool(
            compilerTask.GeneratePathToTool(),
            responseFileCommands,
            commandLineCommands,
            logger,
            (_, _, _) => Task.FromResult<BuildResponse>(new CompletedBuildResponse(0, utf8output: false, output: "")));

        Assert.Equal(0, exitCode);
        Assert.Contains((CompilerServerLogKind.Trace, $"CommandLine = '{commandLineCommands}'"), messages);
        Assert.Contains((CompilerServerLogKind.Trace, $"BuildResponseFile = '{responseFileCommands}'"), messages);
        Assert.Contains(messages, static message =>
            message.Kind == CompilerServerLogKind.Operational &&
            message.Message.StartsWith("CompilerServer: server - server processed compilation - ", StringComparison.Ordinal));

        Assert.DoesNotContain(engine.BuildMessages, message => message.Message?.Contains("CommandLine = '", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(engine.BuildMessages, message => message.Message?.Contains("BuildResponseFile = '", StringComparison.Ordinal) == true);
        Assert.Contains(engine.BuildMessages, message =>
            message.Message?.StartsWith("CompilerServer: server - server processed compilation - ", StringComparison.Ordinal) == true);
    }
}
