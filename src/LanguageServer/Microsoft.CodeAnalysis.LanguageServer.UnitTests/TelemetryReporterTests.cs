// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Tests the language server telemetry reporter without sending events over the network.
/// </summary>
public sealed class TelemetryReporterTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    private ITelemetryReporter CreateReporter(ServerConfiguration serverConfiguration)
    {
        // VS Telemetry requires this environment variable to be set.
        Environment.SetEnvironmentVariable("CommonPropertyBagPath", Path.GetTempFileName());

        var reporter = (ITelemetryReporter?)Activator.CreateInstance(typeof(LanguageServerTelemetryReporter), serverConfiguration, LoggerFactory);
        Assert.NotNull(reporter);
        return reporter;
    }

    private static string GetEventName(string name) => $"test/event/{name}";

    [Fact]
    public void TestVSTelemetryLoadedIntoDefaultAlc()
    {
        var service = CreateReporter(ServerConfigurationWithoutDevKit);
        var assembly = Assembly.GetAssembly(service.GetType());
        Assert.Contains(AssemblyLoadContext.Default.Assemblies, a => a == assembly);
        Assert.Contains(AssemblyLoadContext.Default.Assemblies, a => a.GetName().Name == "Microsoft.VisualStudio.Telemetry");
    }

    [Fact]
    public void TestBlockLogging()
    {
        using var service = CreateReporter(DefaultServerConfiguration);
        service.InitializeSession("off", "test-session", isDefaultSession: false);
        service.LogBlockStart(GetEventName(nameof(TestBlockLogging)), kind: 0, blockId: 0);
        service.LogBlockEnd(blockId: 0, [], CancellationToken.None);
    }

    [Fact]
    public void TestLog()
    {
        using var service = CreateReporter(DefaultServerConfiguration);
        service.InitializeSession("off", "test-session", isDefaultSession: false);
        service.Log(GetEventName(nameof(TestLog)), []);
    }

    [Fact]
    public void TestStandaloneSessionUsesVSDefaultCollectorSettings()
    {
        Environment.SetEnvironmentVariable("CommonPropertyBagPath", Path.GetTempFileName());

        var settings = JsonNode.Parse(Microsoft.VisualStudio.Telemetry.TelemetryService.DefaultSession.SerializeSettings())!.AsObject();

        Assert.Equal(1000, settings["AppId"]!.GetValue<int>());
        Assert.False(string.IsNullOrWhiteSpace(settings["CollectorApiKey"]!.GetValue<string>()));
    }

    [Fact]
    public void TestDevKitSessionPreservesVSCodeSettings()
    {
        var settings = JsonNode.Parse(LanguageServerTelemetryReporter.CreateDevKitSessionSettings("error", "test-session"))!.AsObject();

        Assert.Equal(1010, settings["AppId"]!.GetValue<int>());
        Assert.Equal("test-session", settings["Id"]!.GetValue<string>());
        Assert.Equal("error", settings["TelemetryLevel"]!.GetValue<string>());
        Assert.True(settings["IsInitialSession"]!.GetValue<bool>());
    }
}
