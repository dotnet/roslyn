// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ClientProcessMonitorTests
{
    [Fact]
    public void DaemonMonitorsInitializeProcess()
    {
        var monitor = CreateMonitor(
            AbstractLanguageServerHostTests.ServerConfigurationWithoutDevKit with { IsDaemon = true },
            initializeProcessId: 42);

        Assert.Equal(IClientProcessMonitor.ShutdownStrategy.LSPShutdown, monitor.Strategy);
        Assert.Equal(42, monitor.GetClientProcessId());
    }

    [Fact]
    public void SingleServerCommandLineProcessIsNotMonitoredTwice()
    {
        var monitor = CreateMonitor(
            AbstractLanguageServerHostTests.ServerConfigurationWithoutDevKit with { ClientProcessId = 42 },
            initializeProcessId: 43);

        Assert.Equal(IClientProcessMonitor.ShutdownStrategy.ProcessExit, monitor.Strategy);
        Assert.Null(monitor.GetClientProcessId());
    }

    [Fact]
    public void SingleServerFallsBackToInitializeProcess()
    {
        var monitor = CreateMonitor(
            AbstractLanguageServerHostTests.ServerConfigurationWithoutDevKit,
            initializeProcessId: 42);

        Assert.Equal(42, monitor.GetClientProcessId());
    }

    [Fact]
    public void ServerDoesNotMonitorItself()
    {
        var monitor = CreateMonitor(
            AbstractLanguageServerHostTests.ServerConfigurationWithoutDevKit with { IsDaemon = true },
            initializeProcessId: RoslynLanguageServer.ServerProcessId);

        Assert.Null(monitor.GetClientProcessId());
    }

    private static ClientProcessMonitor CreateMonitor(ServerConfiguration configuration, int initializeProcessId)
    {
        var initializeManager = new InitializeManager();
        initializeManager.SetInitializeParams(new InitializeParams
        {
            ProcessId = initializeProcessId,
            Capabilities = new ClientCapabilities(),
        });

        return new ClientProcessMonitor(configuration, initializeManager);
    }
}
