// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Test using <see cref="AbstractLanguageServerMefHost"/> to ensure telemetry MEF initialization
/// works correctly with the real MEF export provider and composition logic. 
/// </summary>
public sealed class TelemetryReporterTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerMefHost(testOutputHelper)
{
    private async Task<ITelemetryReporter> CreateReporterAsync(TestLspServer testServer)
    {
        // VS Telemetry requires this environment variable to be set.
        Environment.SetEnvironmentVariable("CommonPropertyBagPath", Path.GetTempFileName());

        var reporter = testServer.ExportProvider.GetExport<ITelemetryReporter>().Value;

        Assert.NotNull(reporter);

        // Do not set default session in tests to enable test isolation:
        reporter.InitializeSession("off", "test-session", isDefaultSession: false);

        return reporter;
    }

    private static string GetEventName(string name) => $"test/event/{name}";

    [Fact]
    public async Task TestVSTelemetryLoadedIntoDefaultAlc()
    {
        await using var testServer = await CreateLanguageServerAsync();

        var service = await CreateReporterAsync(testServer);
        var assembly = Assembly.GetAssembly(service.GetType());
        Assert.Contains(AssemblyLoadContext.Default.Assemblies, a => a == assembly);
        Assert.Contains(AssemblyLoadContext.Default.Assemblies, a => a.GetName().Name == "Microsoft.VisualStudio.Telemetry");
    }

    [Fact]
    public async Task TestFault()
    {
        await using var testServer = await CreateLanguageServerAsync();
        var service = await CreateReporterAsync(testServer);
        service.ReportFault(GetEventName(nameof(TestFault)), "test description", logLevel: 2, forceDump: false, processId: 0, new Exception());
    }

    [Fact]
    public async Task TestBlockLogging()
    {
        await using var testServer = await CreateLanguageServerAsync();
        var service = await CreateReporterAsync(testServer);
        service.LogBlockStart(GetEventName(nameof(TestBlockLogging)), kind: 0, blockId: 0);
        service.LogBlockEnd(blockId: 0, [], CancellationToken.None);
    }

    [Fact]
    public async Task TestLog()
    {
        await using var testServer = await CreateLanguageServerAsync();
        var service = await CreateReporterAsync(testServer);
        service.Log(GetEventName(nameof(TestLog)), []);
    }
}
