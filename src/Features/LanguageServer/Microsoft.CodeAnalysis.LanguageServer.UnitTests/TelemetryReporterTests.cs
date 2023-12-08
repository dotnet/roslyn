// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class TelemetryReporterTests : AbstractLanguageServerHostTests
{
    public TelemetryReporterTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    private async Task<ITelemetryReporter> CreateReporterAsync()
    {
        var exportProvider = await LanguageServerTestComposition.CreateExportProviderAsync(TestOutputLogger.Factory, includeDevKitComponents: true);

        var reporter = exportProvider.GetExport<ITelemetryReporter>().Value;
        Assert.NotNull(reporter);

        // Do not set default session in tests to enable test isolation:
        reporter.InitializeSession("off", "test-session", isDefaultSession: false);

        return reporter;
    }

    private static string GetEventName(string name) => $"test/event/{name}";

    [Fact]
    public async Task TestFault()
    {
        var service = await CreateReporterAsync();
        service.ReportFault(GetEventName(nameof(TestFault)), "test description", logLevel: 2, forceDump: false, processId: 0, new Exception());
    }

    [Fact]
    public async Task TestBlockLogging()
    {
        var service = await CreateReporterAsync();
        service.LogBlockStart(GetEventName(nameof(TestBlockLogging)), kind: 0, blockId: 0);
        service.LogBlockEnd(blockId: 0, [], CancellationToken.None);
    }

    [Fact]
    public async Task TestLog()
    {
        var service = await CreateReporterAsync();
        service.Log(GetEventName(nameof(TestLog)), []);
    }
}
