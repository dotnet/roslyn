// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class DaemonPipeNameTests
{
    private const string ToolIdentifier = "/tools/v1/Microsoft.CodeAnalysis.LanguageServer.dll";

    [Fact]
    public void PipeName_IsDeterministic()
    {
        var first = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        var second = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        Assert.Equal(first, second);
    }

    [Fact]
    public void PipeName_DiffersByToolIdentifier()
    {
        var v1 = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v1/server.dll", telemetryLevel: "all");
        var v2 = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v2/server.dll", telemetryLevel: "all");
        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void PipeName_DiffersByUser()
    {
        var user1 = DaemonPipeName.GetPipeName("user1", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        var user2 = DaemonPipeName.GetPipeName("user2", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void PipeName_DiffersByElevation()
    {
        var standard = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        var elevated = DaemonPipeName.GetPipeName("user", isAdmin: true, ToolIdentifier, telemetryLevel: "all");
        Assert.NotEqual(standard, elevated);
    }

    [Fact]
    public void PipeName_NormalizesToolIdentifierCasingOnWindows()
    {
        var mixedCase = DaemonPipeName.GetPipeName("user", isAdmin: false, "/Tools/V1/Server.dll", telemetryLevel: "all");
        var lowerCase = DaemonPipeName.GetPipeName("user", isAdmin: false, "/tools/v1/server.dll", telemetryLevel: "all");
        if (OperatingSystem.IsWindows())
            Assert.Equal(mixedCase, lowerCase);
        else
            Assert.NotEqual(mixedCase, lowerCase);
    }

    [Fact]
    public void PipeName_IsFileSystemAndUrlSafe()
    {
        var name = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain('=', name);
    }

    [Fact]
    public void MutexNames_HaveExpectedShapeAndDiffer()
    {
        var pipeName = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        var serverMutex = DaemonPipeName.GetServerMutexName(pipeName);
        var clientMutex = DaemonPipeName.GetClientMutexName(pipeName);

        Assert.StartsWith(@"Global\", serverMutex);
        Assert.StartsWith(@"Global\", clientMutex);
        Assert.EndsWith(".server", serverMutex);
        Assert.EndsWith(".client", clientMutex);
        Assert.NotEqual(serverMutex, clientMutex);
        Assert.Contains(pipeName, serverMutex);
        Assert.Contains(pipeName, clientMutex);
    }

    [Fact]
    public void PipeName_DiffersByTelemetryLevel()
    {
        var enabled = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "all");
        var disabled = DaemonPipeName.GetPipeName("user", isAdmin: false, ToolIdentifier, telemetryLevel: "off");

        Assert.NotEqual(enabled, disabled);
    }
}
