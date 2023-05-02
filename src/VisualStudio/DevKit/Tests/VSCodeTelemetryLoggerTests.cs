// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.LanguageServer.Services.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Xunit;
using TelemetryService = Microsoft.CodeAnalysis.LanguageServer.Services.Telemetry.TelemetryService;

namespace Microsoft.VisualStudio.LanguageServices.DevKit.UnitTests;

public class VSCodeTelemetryLoggerTests
{
    private static TelemetryService CreateService()
    {
        var assembly = typeof(VSCodeTelemetryLogger).Assembly;
        var telemetryDirectory = new FileInfo(assembly.Location).Directory.FullName;
        var service = TelemetryService.TryCreate("off", telemetryDirectory);

        Assert.NotNull(service);

        return service;
    }

    private static string GetEventName(string name) => $"test/event/{name}";

    [Fact]
    public void TestFault()
    {
        var service = CreateService();
        service.ReportFault(GetEventName(nameof(TestFault)), "test description", (int)FaultSeverity.General, forceDump: false, processId: 0, new Exception());
    }

    [Fact]
    public void TestBlockLogging()
    {
        var service = CreateService();
        service.LogBlockStart(GetEventName(nameof(TestBlockLogging)), kind: 0, blockId: 0);
        service.LogBlockEnd(blockId: 0, ImmutableDictionary<string, object>.Empty, CancellationToken.None);
    }

    [Fact]
    public void TestLog()
    {
        var service = CreateService();
        service.Log(GetEventName(nameof(TestLog)), ImmutableDictionary<string, object>.Empty);
    }
}
