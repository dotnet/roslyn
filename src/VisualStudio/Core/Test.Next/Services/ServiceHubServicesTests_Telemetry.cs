// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.UnitTests.Logging;
using Microsoft.VisualStudio.Telemetry;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

public sealed partial class ServiceHubServicesTests
{
    /// <summary>
    /// Covers the OOP process's half of telemetry setup: initializing a session over the brokered
    /// service configures the remote host, and telemetry logged there reaches the configured sinks.
    /// </summary>
    [Fact]
    public async Task TestRemoteProcessTelemetrySessionInitialization()
    {
        using var workspace = CreateWorkspace();
        using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);

        var logger = new TestTelemetryLogger();
        using var registration = RoslynTelemetry.AddEventSink(logger);

        // Stands in for the settings the VS host serializes across. The collector key is a syntactically
        // valid placeholder and the level is off, so the real sink this also installs in the remote host
        // has nothing to send and nowhere to send it.
        var processStartTime = Process.GetCurrentProcess().StartTime.ToFileTimeUtc();
        var settings = $$"""
            {"Id":"{{Guid.NewGuid()}}","HostName":"Default","AppId":1000,"TelemetryLevel":"off","CollectorApiKey":"00000000000000000000000000000000-00000000-0000-0000-0000-000000000000-0000","ProcessStartTime":{{processStartTime}}}
            """;
        var hostProcessId = Process.GetCurrentProcess().Id;

        var succeeded = await client.TryInvokeAsync<IRemoteProcessTelemetryService>(
            (service, cancellationToken) => service.InitializeTelemetrySessionAsync(
                hostProcessId, settings, logDelta: false, cancellationToken),
            CancellationToken.None);

        Assert.True(succeeded);

        // The remote host logs that it connected as the last step of initialization.
        var connect = Assert.Single(logger.PostedEvents, e => e.Name == "vs/ide/vbcs/remotehost/connect");
        Assert.Equal(hostProcessId, connect.Properties["vs.ide.vbcs.remotehost.connect.host"]);
        Assert.Equal(RuntimeInformation.FrameworkDescription, connect.Properties["vs.ide.vbcs.remotehost.connect.framework"]);
    }
}
