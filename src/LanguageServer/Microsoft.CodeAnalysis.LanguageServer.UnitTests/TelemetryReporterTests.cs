// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Tests the language server telemetry reporter without sending events over the network.
/// </summary>
public sealed class TelemetryReporterTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    private LanguageServerTelemetry CreateReporter(ServerConfiguration serverConfiguration)
    {
        // VS Telemetry requires this environment variable to be set.
        Environment.SetEnvironmentVariable("CommonPropertyBagPath", Path.GetTempFileName());

        var reporter = (LanguageServerTelemetry?)Activator.CreateInstance(typeof(LanguageServerTelemetry), serverConfiguration, LoggerFactory);
        Assert.NotNull(reporter);
        return reporter;
    }

    private static string GetEventName(string name) => $"test/event/{name}";

    [Fact]
    public void TestVSTelemetryLoadedIntoDefaultAlc()
    {
        using var service = CreateReporter(DefaultServerConfiguration);
        service.InitializeSession("off", "test-session", isDefaultSession: false);

        var assembly = Assembly.GetAssembly(service.GetType());
        Assert.Contains(AssemblyLoadContext.Default.Assemblies, a => a == assembly);
        Assert.Contains(AssemblyLoadContext.Default.Assemblies, a => a.GetName().Name == "Microsoft.VisualStudio.Telemetry");
    }

    /// <summary>
    /// Razor's VS Code extension owns no telemetry session and posts through this host's, via
    /// <see cref="TelemetryReporterWrapper"/>. Covers both directions of that bridge.
    /// </summary>
    [Fact]
    public void TestRazorBridgePostsThroughTheHostSession()
    {
        using var service = CreateReporter(DefaultServerConfiguration);
        service.InitializeSession("off", "test-session", isDefaultSession: false);

        // The MEF importing constructor is obsolete-as-error, so construct through Activator.
        var wrapper = (TelemetryReporterWrapper?)Activator.CreateInstance(
            typeof(TelemetryReporterWrapper), new Lazy<LanguageServerTelemetry>(() => service));
        Assert.NotNull(wrapper);

        wrapper.ReportEvent(GetEventName(nameof(TestRazorBridgePostsThroughTheHostSession)), [new("method", "textDocument/hover")]);

        var meter = new VSTelemetryMeterProvider().CreateMeter("test.meter");
        var histogram = meter.CreateHistogram<long>("Duration");
        histogram.Record(42);
        wrapper.ReportMetric(new TelemetryHistogramEvent<long>(new TelemetryEvent(GetEventName("metric")), histogram));
    }

    [Theory]
    [InlineData("all", true)]
    [InlineData("off", false)]
    [InlineData("error", false)]
    [InlineData("crash", false)]
    [InlineData("ALL", false)]
    [InlineData("OFF", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("invalid", false)]
    [InlineData(null, false)]
    public void TestCopilotCliTelemetryLevelFailsClosed(string? telemetryLevel, bool expected)
    {
        Assert.Equal(expected, LanguageServerTelemetry.IsCopilotCliTelemetryEnabled(telemetryLevel));
    }

    [Fact]
    public void TestDevKitSessionPreservesVSCodeSettings()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var processStartTime = currentProcess.StartTime.ToFileTimeUtc();
        var serializedSettings = LanguageServerTelemetry.CreateDevKitSessionSettings("error", "test-session");
        var expectedSettings = $$"""
            {"Id":"test-session","HostName":"Default","TelemetryLevel":"error","IsInitialSession":true,"CollectorApiKey":"0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255","AppId":1010,"ProcessStartTime":{{processStartTime}}}
            """;

        Assert.Equal(expectedSettings, serializedSettings);

        var settings = JsonNode.Parse(serializedSettings)!.AsObject();
        Assert.Equal(1010, settings["AppId"]!.GetValue<int>());
        Assert.Equal("test-session", settings["Id"]!.GetValue<string>());
        Assert.Equal("error", settings["TelemetryLevel"]!.GetValue<string>());
        Assert.True(settings["IsInitialSession"]!.GetValue<bool>());
    }

    [Fact]
    public void TestDevKitSessionPreservesTelemetryLevelValidation()
    {
        using var session = new Microsoft.VisualStudio.Telemetry.TelemetrySession(
            LanguageServerTelemetry.CreateDevKitSessionSettings("invalid", "test-session"));
        session.Start();

        Assert.False(session.IsOptedIn);
    }
}
