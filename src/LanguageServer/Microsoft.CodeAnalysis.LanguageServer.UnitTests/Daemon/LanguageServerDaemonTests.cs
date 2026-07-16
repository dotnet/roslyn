// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipes;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerDaemonTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task Daemon_SecondInstanceOnSamePipe_TryCreateReturnsFalse()
    {
        await using var daemon = await CreateDaemonServerAsync();

        // The daemon holds the server mutex for its pipe...
        Assert.True(daemon.IsRunning);

        // ...so a second daemon on the same pipe must observe it and fail to become the daemon.
        Assert.False(NamedPipeDaemonConnectionSource.TryCreate(daemon.PipeName, LoggerFactory.CreateLogger("Daemon2"), out var secondSource));
        Assert.Null(secondSource);
    }

    [Fact]
    public async Task Daemon_AcceptsClientAndInitializes()
    {
        await using var daemon = await CreateDaemonServerAsync();
        await using var client = await daemon.CreateClientAsync();
        Assert.NotNull(client.ServerCapabilities);
    }

    [Fact]
    public async Task Daemon_ClientDisconnect_WithInfiniteKeepAlive_StaysAlive()
    {
        await using var daemon = await CreateDaemonServerAsync();

        await using (var client = await daemon.CreateClientAsync())
        {
            Assert.NotNull(client.ServerCapabilities);
        }

        // After the only client disconnects, its per-client server tears down...
        await WaitForConditionAsync(() => daemon.GetStartedServers().IsEmpty);

        // ...but with an infinite keepalive the daemon itself keeps running.
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.False(daemon.DaemonExitTask.IsCompleted);
    }

    // Two clients connect to the same daemon concurrently. Each gets its own independent server, so both initialize
    // successfully and neither connection affects the other or the daemon.
    [Fact]
    public async Task Daemon_SecondConcurrentConnection_IsIsolatedFromDaemonAndFirstClient()
    {
        await using var daemon = await CreateDaemonServerAsync();

        await using var first = await daemon.CreateClientAsync();
        Assert.NotNull(first.ServerCapabilities);

        await using var second = await daemon.CreateClientAsync();
        Assert.NotNull(second.ServerCapabilities);

        // Both clients have their own independent server, and the daemon stays up.
        Assert.False(daemon.DaemonExitTask.IsCompleted);
        Assert.Equal(2, daemon.GetStartedServers().Length);
    }

    // Each connected client gets its own server with its own Host workspace. A project loaded into one server's
    // workspace must not be visible to the other server, and each server's registration service must track only
    // its own workspaces.
    [Fact]
    public async Task Daemon_EachServerLoadsProjectsInItsOwnWorkspace()
    {
        await using var daemon = await CreateDaemonServerAsync();
        await using var first = await daemon.CreateClientAsync();
        await using var second = await daemon.CreateClientAsync();

        var factory1 = first.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        var factory2 = second.GetRequiredLspService<LanguageServerWorkspaceFactory>();

        // Each server has its own, distinct Host workspace instance.
        Assert.NotSame(factory1.HostWorkspace, factory2.HostWorkspace);

        // Load a distinct project into each server's Host workspace.
        LoadProject(factory1, "ProjectOne");
        LoadProject(factory2, "ProjectTwo");

        // Each server only sees its own project; loading into one server does not leak into the other.
        Assert.Contains(factory1.HostWorkspace.CurrentSolution.Projects, p => p.Name == "ProjectOne");
        Assert.DoesNotContain(factory1.HostWorkspace.CurrentSolution.Projects, p => p.Name == "ProjectTwo");
        Assert.Contains(factory2.HostWorkspace.CurrentSolution.Projects, p => p.Name == "ProjectTwo");
        Assert.DoesNotContain(factory2.HostWorkspace.CurrentSolution.Projects, p => p.Name == "ProjectOne");

        // Each server's registration service tracks its own Host workspace and not the other server's.
        var registrations1 = first.GetRequiredLspService<LspWorkspaceRegistrationService>().GetAllRegistrations();
        var registrations2 = second.GetRequiredLspService<LspWorkspaceRegistrationService>().GetAllRegistrations();
        Assert.Contains(factory1.HostWorkspace, registrations1);
        Assert.DoesNotContain(factory2.HostWorkspace, registrations1);
        Assert.Contains(factory2.HostWorkspace, registrations2);
        Assert.DoesNotContain(factory1.HostWorkspace, registrations2);
    }

    // If one client's server faults (e.g. the client process crashes and abruptly drops its connection), the daemon
    // tears down only that server. The daemon stays alive, the other client's server is untouched and still
    // functional, and the daemon keeps accepting new clients.
    [Fact]
    public async Task Daemon_OneClientCrashing_DoesNotAffectDaemonOrOtherClients()
    {
        await using var daemon = await CreateDaemonServerAsync();
        await using var survivor = await daemon.CreateClientAsync();
        await using var crashing = await daemon.CreateClientAsync();

        Assert.Equal(2, daemon.GetStartedServers().Length);

        // Simulate the second client process crashing: abruptly drop its transport with no clean LSP shutdown.
        await crashing.CrashAsync();

        // The daemon tears down only the crashed client's server and keeps running.
        await WaitForConditionAsync(() => daemon.GetStartedServers().Length == 1);
        Assert.False(daemon.DaemonExitTask.IsCompleted);

        // Load a document into the surviving server, then issue a real LSP request through the surviving client's
        // JSON-RPC connection. This verifies that the crash did not just leave the server object alive, but that its
        // request loop and response path are still functional.
        var survivorFactory = survivor.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        var survivorDocumentUri = LoadProjectWithDocument(survivorFactory, "Survivor");
        var hover = await survivor.ExecuteRequestAsync<HoverParams, Hover>(
            Methods.TextDocumentHoverName,
            new HoverParams
            {
                TextDocument = new TextDocumentIdentifier { DocumentUri = survivorDocumentUri },
                Position = new Position(0, 6),
            },
            CancellationToken.None);

        Assert.NotNull(hover);
        Assert.Contains("Survivor", hover.Contents.Fourth.Value);

        // And the daemon still accepts brand-new clients.
        await using var late = await daemon.CreateClientAsync();
        Assert.NotNull(late.ServerCapabilities);
    }

    [Fact]
    public async Task Daemon_ShortKeepalive_DoesNotFireBeforeFirstConnectionOrWhileClientIsActive()
    {
        await using var daemon = await CreateDaemonServerAsync(keepAlive: TimeSpan.FromSeconds(1));

        // The configured keepalive is shorter than this delay, but the daemon uses its separate initial-connection
        // timeout after signaling readiness.
        await Task.Delay(TimeSpan.FromSeconds(3));
        Assert.False(daemon.DaemonExitTask.IsCompleted);

        await using (var client = await daemon.CreateClientAsync())
        {
            Assert.NotNull(client.ServerCapabilities);
            await Task.Delay(TimeSpan.FromSeconds(3));
            Assert.False(daemon.DaemonExitTask.IsCompleted);
        }

        await daemon.DaemonExitTask.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(daemon.IsRunning);
    }

    [Fact]
    public async Task Daemon_ZeroKeepalive_ExitsImmediatelyAfterFirstClientDisconnects()
    {
        await using var daemon = await CreateDaemonServerAsync(keepAlive: TimeSpan.Zero);

        await using (var client = await daemon.CreateClientAsync())
        {
            Assert.NotNull(client.ServerCapabilities);
        }

        await daemon.DaemonExitTask.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(daemon.IsRunning);
    }

    [Fact]
    public async Task Daemon_StartupFailure_DaemonSurvivesAndContinuesAccepting()
    {
        await using var daemon = await CreateDaemonServerAsync();

        var startAttempts = 0;
        daemon.GetConnectionManagerTestAccessor().OnBeforeStartServer = () =>
        {
            if (Interlocked.Increment(ref startAttempts) == 1)
                throw new InvalidOperationException("Simulated startup failure for test.");
        };

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await using var failedClient = await daemon.CreateClientAsync();
        });

        await WaitForConditionAsync(() => startAttempts >= 1);
        Assert.False(daemon.DaemonExitTask.IsCompleted);

        daemon.GetConnectionManagerTestAccessor().OnBeforeStartServer = null;
        await using var client = await daemon.CreateClientAsync();
        Assert.NotNull(client.ServerCapabilities);
    }

    [Fact]
    public async Task Daemon_SlowServerStartup_DoesNotBlockAcceptingNextConnection()
    {
        await using var daemon = await CreateDaemonServerAsync();
        using var firstStartEntered = new ManualResetEventSlim();
        using var releaseFirstStart = new ManualResetEventSlim();

        var startAttempts = 0;
        daemon.GetConnectionManagerTestAccessor().OnBeforeStartServer = () =>
        {
            if (Interlocked.Increment(ref startAttempts) == 1)
            {
                firstStartEntered.Set();
                releaseFirstStart.Wait();
            }
        };

        using var firstClient = NamedPipeUtil.CreateClient(
            serverName: ".",
            daemon.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await firstClient.ConnectAsync(timeout: 30_000);
            Assert.True(firstStartEntered.Wait(TimeSpan.FromSeconds(30)));

            await using var secondClient = await daemon.CreateClientAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert.NotNull(secondClient.ServerCapabilities);
        }
        finally
        {
            daemon.GetConnectionManagerTestAccessor().OnBeforeStartServer = null;
            releaseFirstStart.Set();
        }
    }

    private static void LoadProject(LanguageServerWorkspaceFactory workspaceFactory, string projectName)
        => workspaceFactory.HostWorkspace.SetCurrentSolution(
            solution => solution.AddProject(projectName, projectName, LanguageNames.CSharp).Solution,
            WorkspaceChangeKind.ProjectAdded);

    private static DocumentUri LoadProjectWithDocument(LanguageServerWorkspaceFactory workspaceFactory, string typeName)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");
        workspaceFactory.HostWorkspace.SetCurrentSolution(
            solution => solution
                .AddProject(typeName, typeName, LanguageNames.CSharp)
                .AddDocument(Path.GetFileName(filePath), SourceText.From($"class {typeName} {{ }}"), filePath: filePath)
                .Project.Solution,
            WorkspaceChangeKind.ProjectAdded);

        return ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
    }
}
