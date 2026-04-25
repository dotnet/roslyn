// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.Editor.Razor.Test.Shared;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.Telemetry;

public class TelemetryReporterTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void NoArgument()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        reporter.ReportEvent("EventName", Severity.Normal);

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.False(actualEvent.HasProperties);
    }

    [Fact]
    public void OneArgument()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        reporter.ReportEvent("EventName", Severity.Normal, new Property("P1", false));

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);
        Assert.Single(actualEvent.Properties);

        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
    }

    [Fact]
    public void TwoArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        reporter.ReportEvent("EventName", Severity.Normal, new("P1", false), new("P2", "test"));

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
        Assert.Equal("test", actualEvent.Properties["dotnet.razor.p2"]);
    }

    [Fact]
    public void ThreeArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var p3Value = Guid.NewGuid();
        reporter.ReportEvent("EventName",
            Severity.Normal,
            new("P1", false),
            new("P2", "test"),
            new("P3", p3Value));

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
        Assert.Equal("test", actualEvent.Properties["dotnet.razor.p2"]);

        var p3 = Assert.IsType<TelemetryComplexProperty>(actualEvent.Properties["dotnet.razor.p3"]);
        Assert.Equal(p3Value, p3.Value);
    }

    [Fact]
    public void FourArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var p3Value = Guid.NewGuid();
        reporter.ReportEvent("EventName",
            Severity.Normal,
            new("P1", false),
            new("P2", "test"),
            new("P3", p3Value),
            new("P4", 100));

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
        Assert.Equal("test", actualEvent.Properties["dotnet.razor.p2"]);

        var p3 = Assert.IsType<TelemetryComplexProperty>(actualEvent.Properties["dotnet.razor.p3"]);
        Assert.Equal(p3Value, p3.Value);

        Assert.Equal(100, actualEvent.Properties["dotnet.razor.p4"]);
    }

    [Fact]
    public void Block_NoArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        using (var scope = reporter.BeginBlock("EventName", Severity.Normal))
        {
        }

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.True(actualEvent.HasProperties);

        Assert.IsType<long>(actualEvent.Properties["dotnet.razor.eventscope.ellapsedms"]);
    }

    [Fact]
    public void Block_OneArgument()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        using (reporter.BeginBlock("EventName", Severity.Normal, new Property("P1", false)))
        {
        }

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.IsType<long>(actualEvent.Properties["dotnet.razor.eventscope.ellapsedms"]);
        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
    }

    [Fact]
    public void Block_TwoArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        using (reporter.BeginBlock("EventName", Severity.Normal, TimeSpan.Zero, new("P1", false), new("P2", "test")))
        {
        }

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.IsType<long>(actualEvent.Properties["dotnet.razor.eventscope.ellapsedms"]);
        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
        Assert.Equal("test", actualEvent.Properties["dotnet.razor.p2"]);
    }

    [Fact]
    public void Block_ThreeArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var p3Value = Guid.NewGuid();
        using (reporter.BeginBlock("EventName",
            Severity.Normal,
            TimeSpan.Zero,
            new("P1", false),
            new("P2", "test"),
            new("P3", p3Value)))
        {
        }

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.IsType<long>(actualEvent.Properties["dotnet.razor.eventscope.ellapsedms"]);
        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
        Assert.Equal("test", actualEvent.Properties["dotnet.razor.p2"]);

        var p3 = actualEvent.Properties["dotnet.razor.p3"] as TelemetryComplexProperty;
        Assert.NotNull(p3);
        Assert.Equal(p3Value, p3.Value);
    }

    [Fact]
    public void Block_FourArguments()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var p3Value = Guid.NewGuid();
        using (reporter.BeginBlock("EventName",
            Severity.Normal,
            TimeSpan.Zero,
            new("P1", false),
            new("P2", "test"),
            new("P3", p3Value),
            new("P4", 100)))
        {
        }

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/eventname", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.IsType<long>(actualEvent.Properties["dotnet.razor.eventscope.ellapsedms"]);
        Assert.Equal(false, actualEvent.Properties["dotnet.razor.p1"]);
        Assert.Equal("test", actualEvent.Properties["dotnet.razor.p2"]);

        var p3 = actualEvent.Properties["dotnet.razor.p3"] as TelemetryComplexProperty;
        Assert.NotNull(p3);
        Assert.Equal(p3Value, p3.Value);

        Assert.Equal(100, actualEvent.Properties["dotnet.razor.p4"]);
    }

    [Fact]
    public void HandleRIEWithInnerException()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);

        var ae = new ApplicationException("expectedText");
        var rie = new RemoteInvocationException("a", 0, ae);

        reporter.ReportFault(rie, rie.Message);

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.High, actualEvent.Severity);
        Assert.Equal("dotnet/razor/fault", actualEvent.Name);
        // faultEvent doesn't expose any interesting properties,
        // like the ExceptionObject, or the resulting Description,
        // or really anything we would explicitly want to verify against.
        Assert.IsType<FaultEvent>(actualEvent);
    }

    [Fact]
    public void HandleRIEWithNoInnerException()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);

        var rie = new RemoteInvocationException("a", 0, errorData: null);

        reporter.ReportFault(rie, rie.Message);

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.High, actualEvent.Severity);
        Assert.Equal("dotnet/razor/fault", actualEvent.Name);
        // faultEvent doesn't expose any interesting properties,
        // like the ExceptionObject, or the resulting Description,
        // or really anything we would explicitly want to verify against.
        Assert.IsType<FaultEvent>(actualEvent);
    }

    [Fact]
    public void TrackLspRequest()
    {
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var correlationId = Guid.NewGuid();
        using (reporter.TrackLspRequest("MethodName", "ServerName", TimeSpan.Zero, correlationId))
        {
        }

        var actualEvent = Assert.Single(reporter.Events);
        Assert.Equal(TelemetrySeverity.Normal, actualEvent.Severity);
        Assert.Equal("dotnet/razor/tracklsprequest", actualEvent.Name);
        Assert.True(actualEvent.HasProperties);

        Assert.IsType<long>(actualEvent.Properties["dotnet.razor.eventscope.ellapsedms"]);
        Assert.Equal("MethodName", actualEvent.Properties["dotnet.razor.eventscope.method"]);
        Assert.Equal("ServerName", actualEvent.Properties["dotnet.razor.eventscope.languageservername"]);

        var correlationProperty = actualEvent.Properties["dotnet.razor.eventscope.correlationid"] as TelemetryComplexProperty;
        Assert.NotNull(correlationProperty);
        Assert.Equal(correlationId, correlationProperty.Value);
    }

    [Fact]
    public void ReportFault_OperationCanceledExceptionWithoutInnerException_SkipsFaultReport()
    {
        // Arrange
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var exception = new OperationCanceledException("OCE", innerException: null);

        // Act
        reporter.ReportFault(exception, "Test message");

        // Assert
        Assert.Empty(reporter.Events);
    }

    [Fact]
    public void ReportFault_TaskCanceledExceptionWithoutInnerException_SkipsFaultReport()
    {
        // Arrange
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var exception = new TaskCanceledException("TCE", innerException: null);

        // Act
        reporter.ReportFault(exception, "Test message");

        // Assert
        Assert.Empty(reporter.Events);
    }

    [Fact]
    public void ReportFault_InnerExceptionOfOCEIsNotAnOCE_ReportsFault()
    {
        // Arrange
        var depth = 3;
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var innerMostException = new Exception();
        var exception = new OperationCanceledException("Test", innerMostException);
        for (var i = 0; i < depth; i++)
        {
            exception = new OperationCanceledException("Test", exception);
        }

        // Act
        reporter.ReportFault(exception, "Test message");

        // Assert
        Assert.NotEmpty(reporter.Events);
    }

    [Fact]
    public void ReportFault_InnerMostExceptionIsOperationCanceledException_SkipsFaultReport()
    {
        // Arrange
        var depth = 3;
        using var reporter = new TestTelemetryReporter(LoggerFactory);
        var innerMostException = new OperationCanceledException();
        var exception = new OperationCanceledException("Test", innerMostException);
        for (var i = 0; i < depth; i++)
        {
            exception = new OperationCanceledException("Test", exception);
        }

        // Act
        reporter.ReportFault(exception, "Test message");

        // Assert
        Assert.Empty(reporter.Events);
    }

    [Fact]
    public void ReportHistogram()
    {
        // Arrange
        var reporter = new TestTelemetryReporter(LoggerFactory);

        // Act
        reporter.ReportRequestTiming(
            Methods.TextDocumentCodeActionName,
            WellKnownLspServerKinds.RazorLspServer.GetContractName(),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            CodeAnalysis.Razor.Telemetry.TelemetryResult.Succeeded);

        reporter.ReportRequestTiming(
            Methods.TextDocumentCodeActionName,
            WellKnownLspServerKinds.RazorLspServer.GetContractName(),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(200),
            CodeAnalysis.Razor.Telemetry.TelemetryResult.Cancelled);

        reporter.ReportRequestTiming(
            Methods.TextDocumentCodeActionName,
            WellKnownLspServerKinds.RazorLspServer.GetContractName(),
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(300),
            CodeAnalysis.Razor.Telemetry.TelemetryResult.Failed);

        reporter.ReportRequestTiming(
            Methods.TextDocumentCompletionName,
             WellKnownLspServerKinds.RazorLspServer.GetContractName(),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            CodeAnalysis.Razor.Telemetry.TelemetryResult.Succeeded);

        reporter.ReportRequestTiming(
            Methods.TextDocumentCompletionName,
             WellKnownLspServerKinds.RazorLspServer.GetContractName(),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(200),
            CodeAnalysis.Razor.Telemetry.TelemetryResult.Cancelled);

        reporter.ReportRequestTiming(
            Methods.TextDocumentCompletionName,
             WellKnownLspServerKinds.RazorLspServer.GetContractName(),
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(300),
            CodeAnalysis.Razor.Telemetry.TelemetryResult.Failed);

        reporter.Dispose();

        // Assert
        reporter.AssertMetrics(
            static evt =>
            {
                var histogram = Assert.IsAssignableFrom<IHistogram<long>>(evt.Instrument);
                Assert.Equal("TimeInQueue", histogram.Name);

                var telemetryEvent = evt.Event;
                Assert.Equal("dotnet/razor/lsp_timeinqueue", telemetryEvent.Name);

                var prop = Assert.Single(telemetryEvent.Properties);
                Assert.Equal("dotnet.razor.method", prop.Key);
                Assert.Equal(Methods.TextDocumentCodeActionName, prop.Value);
            },
            static evt =>
            {
                var histogram = Assert.IsAssignableFrom<IHistogram<long>>(evt.Instrument);
                Assert.Equal(Methods.TextDocumentCodeActionName, histogram.Name);

                var telemetryEvent = evt.Event;
                Assert.Equal("dotnet/razor/lsp_requestduration", telemetryEvent.Name);

                var prop = Assert.Single(telemetryEvent.Properties);
                Assert.Equal("dotnet.razor.method", prop.Key);
                Assert.Equal(Methods.TextDocumentCodeActionName, prop.Value);
            },
            static evt =>
            {
                var histogram = Assert.IsAssignableFrom<IHistogram<long>>(evt.Instrument);
                Assert.Equal(Methods.TextDocumentCompletionName, histogram.Name);

                var telemetryEvent = evt.Event;
                Assert.Equal("dotnet/razor/lsp_requestduration", telemetryEvent.Name);

                var prop = Assert.Single(telemetryEvent.Properties);
                Assert.Equal("dotnet.razor.method", prop.Key);
                Assert.Equal(Methods.TextDocumentCompletionName, prop.Value);
            });

        Assert.Collection(reporter.Events,
            static evt =>
            {
                Assert.Equal("dotnet/razor/lsp_requestcounter", evt.Name);
                Assert.Collection(evt.Properties,
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.method", prop.Key);
                        Assert.Equal(Methods.TextDocumentCodeActionName, prop.Value);
                    },
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.successful", prop.Key);
                        Assert.Equal(1, prop.Value);
                    },
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.failed", prop.Key);
                        Assert.Equal(1, prop.Value);
                    },
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.cancelled", prop.Key);
                        Assert.Equal(1, prop.Value);
                    });
            },
            static evt =>
            {
                Assert.Equal("dotnet/razor/lsp_requestcounter", evt.Name);
                Assert.Collection(evt.Properties,
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.method", prop.Key);
                        Assert.Equal(Methods.TextDocumentCompletionName, prop.Value);
                    },
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.successful", prop.Key);
                        Assert.Equal(1, prop.Value);
                    },
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.failed", prop.Key);
                        Assert.Equal(1, prop.Value);
                    },
                    static prop =>
                    {
                        Assert.Equal("dotnet.razor.cancelled", prop.Key);
                        Assert.Equal(1, prop.Value);
                    });
            });
    }

    [Fact]
    public void GetModifiedFaultParameters_FiltersCorrectly()
    {
        // The expected module name should be the primary module of this test assembly.
        // The expected method name should be AssumeNotNullFailure() below.
        var currentModule = Assert.Single(Assembly.GetExecutingAssembly().Modules);
        var expectedModuleName = Path.GetFileNameWithoutExtension(currentModule.Name);
        var expectedMethodName = nameof(AssumeNotNullFailure);

        var exception = Assert.Throws<InvalidOperationException>(AssumeNotNullFailure);

        var (moduleName, methodName) = TestTelemetryReporter.GetModifiedFaultParameters(exception);

        Assert.Equal(expectedModuleName, moduleName);
        Assert.Equal(expectedMethodName, methodName);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssumeNotNullFailure()
    {
        ((object?)null).AssumeNotNull();
    }
}
