// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class TaskCompilerServerLoggerTests
{
    [Fact]
    public void RoutesByKind()
    {
        var engine = new MockEngine();
        var task = new Csc { BuildEngine = engine };
        var innerMessages = new List<(CompilerServerLogKind Kind, string Message)>();
        var inner = new TestableCompilerServerLogger
        {
            LogFunc = (kind, message) => innerMessages.Add((kind, message)),
        };
        var logger = new TaskCompilerServerLogger(new TaskLoggingHelper(task), inner);

        logger.Log("trace");
        logger.LogOperational("operational");

        Assert.Equal(
            [
                (CompilerServerLogKind.Trace, "trace"),
                (CompilerServerLogKind.Operational, "operational"),
            ],
            innerMessages);
        Assert.DoesNotContain(engine.BuildMessages, message => message.Message == "trace");
        Assert.Contains(engine.BuildMessages, message => message.Message == "operational");
    }

    [Fact]
    public void DisabledTraceDoesNotFormatOrRoute()
    {
        var engine = new MockEngine();
        var task = new Csc { BuildEngine = engine };
        var inner = new TestableCompilerServerLogger
        {
            IsEnabledFunc = static _ => false,
            LogFunc = delegate { throw new Xunit.Sdk.XunitException("Disabled logger should not be called."); },
        };
        var logger = new TaskCompilerServerLogger(new TaskLoggingHelper(task), inner);
        var argument = new ThrowingFormatArgument();

        logger.Log("trace {0}", argument);
        logger.LogOperational("operational");

        Assert.DoesNotContain(engine.BuildMessages, message => message.Message?.Contains("trace") == true);
        Assert.Contains(engine.BuildMessages, message => message.Message == "operational");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/85244")]
    public void ResponseClassification()
    {
        var cases = new (BuildResponse Response, string Category, string Diagnostic, bool IsFatal)[]
        {
            (new CannotConnectResponse(), "server failed", "cannot connect to the server", false),
            (new RejectedBuildResponse("reason"), "server failed", "server rejected the request 'reason'", false),
            (new AnalyzerInconsistencyBuildResponse(new ReadOnlyCollection<string>(["first", "second"])), "server failed", "server rejected the request due to analyzer / generator issues 'first, second'", false),
            (new IncorrectHashBuildResponse(), "fatal error", "server reports different hash version than build task", true),
            (new ShutdownBuildResponse(0), "server failed", "server gave an unrecognized response", false),
        };

        foreach (var (response, category, diagnostic, isFatal) in cases)
        {
            var engine = new MockEngine();
            var task = new Csc
            {
                BuildEngine = engine,
                ProjectName = "TestProject",
                Sources = MSBuildUtil.CreateTaskItems("test.cs"),
                UseSharedCompilation = true,
            };
            var innerMessages = new List<(CompilerServerLogKind Kind, string Message)>();
            var inner = new TestableCompilerServerLogger
            {
                LogFunc = (kind, message) => innerMessages.Add((kind, message)),
            };
            var logger = new TaskCompilerServerLogger(new TaskLoggingHelper(task), inner);

            var exitCode = task.ExecuteTool(
                task.GeneratePathToTool(),
                task.GenerateResponseFileContents(),
                task.GenerateCommandLineContents(),
                logger,
                (_, _, _) => System.Threading.Tasks.Task.FromResult(response),
                (_, _, _) => 0);

            var expected = $"CompilerServer: {category} - {diagnostic} - TestProject";
            Assert.Equal(0, exitCode);
            if (isFatal)
            {
                Assert.Contains((CompilerServerLogKind.Trace, $"Error: {expected}"), innerMessages);
                Assert.Contains($"ERROR : {expected}", engine.Log);
            }
            else
            {
                Assert.Contains((CompilerServerLogKind.Operational, expected), innerMessages);
                Assert.Contains(engine.BuildMessages, message => message.Message == expected);
                Assert.DoesNotContain("ERROR ", engine.Log);
            }
        }
    }

    private sealed class ThrowingFormatArgument
    {
        public override string ToString() => throw new Xunit.Sdk.XunitException("Trace argument should not be formatted.");
    }
}
