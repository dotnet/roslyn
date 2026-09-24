// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.VisualStudio.ApplicationInsights;
using Microsoft.VisualStudio.ApplicationInsights.Extensibility;
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
        return new LanguageServerTelemetry(serverConfiguration, LoggerFactory, new RoslynTelemetry());
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestServerCommonProperties(bool useDevKitTelemetry)
    {
        var serverConfiguration = useDevKitTelemetry ? DefaultServerConfiguration : ServerConfigurationWithoutDevKit;
        using var service = CreateReporter(serverConfiguration);
        service.InitializeSession("off", "test-session", isDefaultSession: false);

        var session = Assert.IsType<TelemetrySession>(TelemetryReporterWrapper.GetSession(service.Telemetry));
        Assert.True(session.TryGetCommonPropertyValue(LanguageServerTelemetry.ServerVersionPropertyName, out var serverVersion));
        var expectedServerVersion = typeof(LanguageServerTelemetry).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Equal(expectedServerVersion, Assert.IsType<string>(serverVersion));

        Assert.True(session.TryGetCommonPropertyValue(LanguageServerTelemetry.ServerPackageVersionPropertyName, out var serverPackageVersion));
        Assert.Equal(expectedServerVersion.Split('+')[0], Assert.IsType<string>(serverPackageVersion));

        Assert.True(session.TryGetCommonPropertyValue(LanguageServerTelemetry.ServerPlatformPropertyName, out var serverPlatform));
        var expectedPlatform = OperatingSystem.IsWindows()
            ? "windows"
            : OperatingSystem.IsLinux()
                ? "linux"
                : OperatingSystem.IsMacOS()
                    ? "macos"
                    : "unknown";
        Assert.Equal(expectedPlatform, Assert.IsType<string>(serverPlatform));
    }

    [Theory]
    [InlineData("5.12.0-1.26426.8+3aeb96c9", "5.12.0-1.26426.8")]
    [InlineData("5.12.0-1.26426.8", "5.12.0-1.26426.8")]
    public void TestGetServerPackageVersion(string serverVersion, string expectedPackageVersion)
        => Assert.Equal(expectedPackageVersion, LanguageServerTelemetry.GetServerPackageVersion(serverVersion));

    [Fact]
    public void TestDeviceContextInitializerConfigured()
    {
        Assert.True(File.Exists(Path.Combine(TestPaths.GetLanguageServerDirectory(), "ApplicationInsights.config")));

        using var configuration = TelemetryConfiguration.CreateDefault();
        Assert.Contains(configuration.ContextInitializers, static initializer => initializer is DeviceContextInitializer);

        var client = new TelemetryClient(configuration);
        Assert.True(SpinWait.SpinUntil(
            () => !string.IsNullOrEmpty(client.Context.Device.OperatingSystem),
            TimeSpan.FromSeconds(5)));
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
        var wrapper = (TelemetryReporterWrapper?)Activator.CreateInstance(typeof(TelemetryReporterWrapper));
        Assert.NotNull(wrapper);

        using var _ = RoslynTelemetry.SetCurrent(service.Telemetry);
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

    [Fact]
    public void TestStandaloneSessionUsesVisualStudioCollector()
    {
        var serializedSettings = LanguageServerTelemetry.CreateStandaloneSessionSettings("invalid\"level", "test\\session");
        var settings = JsonNode.Parse(serializedSettings)!.AsObject();

        Assert.Equal("f3e86b4023cc43f0be495508d51f588a-f70d0e59-0fb0-4473-9f19-b4024cc340be-7296", settings["CollectorApiKey"]!.GetValue<string>());
        Assert.Equal("invalid\"level", settings["TelemetryLevel"]!.GetValue<string>());
        Assert.Equal("test\\session", settings["Id"]!.GetValue<string>());
    }
}
