// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        logger.LogTrace("trace");
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
    public void DisabledTraceDoesNotRoute()
    {
        var engine = new MockEngine();
        var task = new Csc { BuildEngine = engine };
        var inner = new TestableCompilerServerLogger
        {
            IsEnabledFunc = static _ => false,
            LogFunc = delegate { throw new Xunit.Sdk.XunitException("Disabled logger should not be called."); },
        };
        var logger = new TaskCompilerServerLogger(new TaskLoggingHelper(task), inner);
        logger.LogTrace("trace");
        logger.LogOperational("operational");

        Assert.DoesNotContain(engine.BuildMessages, message => message.Message?.Contains("trace") == true);
        Assert.Contains(engine.BuildMessages, message => message.Message == "operational");
    }

    [Fact]
    public void ExtensionsForwardWithoutCheckingIsEnabled()
    {
        var messages = new List<(CompilerServerLogKind Kind, string Message)>();
        var logger = new TestableCompilerServerLogger
        {
            IsEnabledFunc = static _ => throw new Xunit.Sdk.XunitException("Extensions should not check IsEnabled."),
            LogFunc = (kind, message) => messages.Add((kind, message)),
        };
        var exception = new InvalidOperationException("outer", new Exception("inner"));

        logger.LogTrace("trace");
        logger.LogTrace("trace {0}", 1);
        logger.LogOperational("operational");
        logger.LogOperational("operational {0}", 2);
        logger.LogTraceError("failure");
        logger.LogTraceError("failure {0}", 3);
        logger.LogTraceException(exception, "trace reason");
        logger.LogOperationalException(exception, "operational reason");

        Assert.Equal(8, messages.Count);
        Assert.Equal((CompilerServerLogKind.Trace, "trace"), messages[0]);
        Assert.Equal((CompilerServerLogKind.Trace, "trace 1"), messages[1]);
        Assert.Equal((CompilerServerLogKind.Operational, "operational"), messages[2]);
        Assert.Equal((CompilerServerLogKind.Operational, "operational 2"), messages[3]);
        Assert.Equal((CompilerServerLogKind.Trace, "Error: failure"), messages[4]);
        Assert.Equal((CompilerServerLogKind.Trace, "Error: failure 3"), messages[5]);
        Assert.Equal(CompilerServerLogKind.Trace, messages[6].Kind);
        Assert.Contains("Exception: 'InvalidOperationException' 'outer' occurred during 'trace reason'", messages[6].Message);
        Assert.Contains("Inner exception[0]  Exception: 'Exception' 'inner'", messages[6].Message);
        Assert.Equal(CompilerServerLogKind.Operational, messages[7].Kind);
        Assert.Contains("Exception: 'InvalidOperationException' 'outer' occurred during 'operational reason'", messages[7].Message);
        Assert.Contains("Inner exception[0]  Exception: 'Exception' 'inner'", messages[7].Message);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/85244")]
    public void ResponseClassification()
    {
        var cases = new (BuildResponse? Response, string Category, string Diagnostic, bool IsFatal)[]
        {
            (null, "server failed", "could not launch server", false),
            (new CannotConnectResponse(), "server failed", "cannot connect to the server", false),
            (new RejectedBuildResponse("reason"), "server failed", "server rejected the request 'reason'", false),
            (new AnalyzerInconsistencyBuildResponse(new ReadOnlyCollection<string>(["first", "second"])), "server failed", "server rejected the request due to analyzer / generator issues 'first, second'", false),
            (new IncorrectHashBuildResponse(), "fatal error", "server reports different hash version than build task", true),
            (new ShutdownBuildResponse(0), "server failed", "server gave an unrecognized response", false),
        };

        foreach (var (response, category, diagnostic, isFatal) in cases)
        {
            var engine = new MockEngine();
            var task = new Csc { BuildEngine = engine };
            var innerMessages = new List<(CompilerServerLogKind Kind, string Message)>();
            var inner = new TestableCompilerServerLogger
            {
                LogFunc = (kind, message) => innerMessages.Add((kind, message)),
            };
            var logger = new TaskCompilerServerLogger(new TaskLoggingHelper(task), inner);

            var exitCode = task.HandleResponse("TestProject", response, logger);

            var expected = $"CompilerServer: {category} - {diagnostic} - TestProject";
            Assert.Null(exitCode);
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
}
