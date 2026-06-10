// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

public sealed class DaemonServerLifecycleTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task MultipleClients_ShareOneDaemon_AndEachShutsDownCleanly(bool useNamedPipe)
    {
        await using var daemon = CreateDaemon();

        await using var first = await daemon.CreateClientAsync(useNamedPipe);
        await using var second = await daemon.CreateClientAsync(useNamedPipe);
        await using var third = await daemon.CreateClientAsync(useNamedPipe);

        // Every client is served by the same daemon process (the single-instance guarantee), each over its own
        // editor<->thin-client and thin-client<->daemon transports.
        var daemonProcessId = await daemon.GetDaemonProcessIdAsync(first);
        Assert.Equal(daemonProcessId, await daemon.GetDaemonProcessIdAsync(second));
        Assert.Equal(daemonProcessId, await daemon.GetDaemonProcessIdAsync(third));
        Assert.True(daemon.IsRunning);

        // Each client shuts down cleanly with a success exit code; shutting one down does not disturb the others.
        Assert.Equal(0, await CleanShutdownAsync(first));
        Assert.Equal(0, await CleanShutdownAsync(second));
        Assert.Equal(0, await CleanShutdownAsync(third));
    }

    [Theory, CombinatorialData]
    public async Task OneThinClientKilled_OnlyThatConnectionTornDown(bool useNamedPipe)
    {
        await using var daemon = CreateDaemon();

        // The first client launches the daemon, so killing its thin client also exercises the daemon outliving its
        // launcher.
        await using var victim = await daemon.CreateClientAsync(useNamedPipe);
        await using var survivor = await daemon.CreateClientAsync(useNamedPipe);
        var daemonProcessId = await daemon.GetDaemonProcessIdAsync(survivor);

        // Kill the thin client process (and tree) for the victim client.  The daemon should stay alive and connected
        // to the surviving client.
        victim.ThinClientProcess.Kill(entireProcessTree: true);
        await victim.ThinClientProcess.WaitForExitAsync();

        // The daemon and the surviving client are unaffected: the daemon stays up and the survivor still serves
        // requests, so it can complete a clean shutdown.
        Assert.True(daemon.IsRunning);
        AssertProcessAlive(daemonProcessId);
        Assert.Equal(0, await CleanShutdownAsync(survivor));
    }

    [Theory, CombinatorialData]
    public async Task OneEditorKilled_OnlyThatClientTornDown(bool useNamedPipe)
    {
        await using var daemon = CreateDaemon();

        // The daemon monitors each client's editor via the LSP 'initialize' processId; stand up a killable process to
        // play the editor for the victim client (the test process can't kill itself).
        using var editor = StartIdleEditorProcess();

        await using var victim = await daemon.CreateClientAsync(useNamedPipe, initializeProcessId: editor.Id);
        await using var survivor = await daemon.CreateClientAsync(useNamedPipe);
        var daemonProcessId = await daemon.GetDaemonProcessIdAsync(survivor);

        editor.Kill();

        // The daemon performs a scoped LSP shutdown of only the victim's server, so the victim's thin client loses its
        // server connection and exits non-zero...
        Assert.NotEqual(0, await WaitForThinClientExitAsync(victim));

        // ...while the daemon and the surviving client are untouched.
        Assert.True(daemon.IsRunning);
        AssertProcessAlive(daemonProcessId);
        Assert.Equal(0, await CleanShutdownAsync(survivor));
    }

    [Theory, CombinatorialData]
    public async Task DaemonKilled_EveryThinClientExitsNonZero(bool useNamedPipe)
    {
        await using var daemon = CreateDaemon();

        await using var first = await daemon.CreateClientAsync(useNamedPipe);
        await using var second = await daemon.CreateClientAsync(useNamedPipe);

        var daemonProcess = await first.GetServerProcessAsync();

        // Killing the shared daemon drops every client's connection at once.
        daemonProcess.Kill(entireProcessTree: true);
        await daemonProcess.WaitForExitAsync();

        // Each thin client surfaces the lost daemon as a non-zero exit; it can't restart since it holds no state.
        Assert.NotEqual(0, await WaitForThinClientExitAsync(first));
        Assert.NotEqual(0, await WaitForThinClientExitAsync(second));
        Assert.False(daemon.IsRunning);
    }

    [Fact]
    public async Task KeepAlive_DaemonReusedWithinWindow_ThenExitsWhenIdle()
    {
        var keepAlive = TimeSpan.FromSeconds(15);
        await using var daemon = CreateDaemon(keepAlive);

        await using var first = await daemon.CreateClientAsync(useNamedPipe: true);
        var daemonProcessId = await daemon.GetDaemonProcessIdAsync(first);

        // Cleanly disconnect the only client; the daemon stays alive through its keepalive window.
        Assert.Equal(0, await CleanShutdownAsync(first));
        Assert.True(daemon.IsRunning);

        // A new client connecting during that window reuses the very same daemon process.
        await using var second = await daemon.CreateClientAsync(useNamedPipe: true);
        Assert.Equal(daemonProcessId, await daemon.GetDaemonProcessIdAsync(second));

        // Once the last client disconnects and the keepalive elapses with no clients, the daemon exits on its own.
        Assert.Equal(0, await CleanShutdownAsync(second));
        using var daemonProcess = Process.GetProcessById(daemonProcessId);
        await daemonProcess.WaitForExitAsync();
        Assert.True(daemonProcess.HasExited);
        Assert.False(daemon.IsRunning);
    }

    [Fact]
    public async Task TwoClientsRacingToStart_ShareExactlyOneDaemon()
    {
        await using var daemon = CreateDaemon();

        // Start both clients at once so they race the client mutex's "check server, launch if absent" sequence. The
        // mutex serializes them, so exactly one daemon is launched and both connect to it.
        var firstTask = daemon.CreateClientAsync(useNamedPipe: true);
        var secondTask = daemon.CreateClientAsync(useNamedPipe: true);
        await Task.WhenAll(firstTask, secondTask);
        await using var first = await firstTask;
        await using var second = await secondTask;

        Assert.Equal(
            await daemon.GetDaemonProcessIdAsync(first),
            await daemon.GetDaemonProcessIdAsync(second));
        Assert.True(daemon.IsRunning);
    }

    [Fact]
    public async Task ClientMutexHeld_ConnectingClientSerializesUntilReleased()
    {
        await using var daemon = CreateDaemon();

        using var mutexAcquired = new ManualResetEventSlim();
        using var releaseMutex = new ManualResetEventSlim();

        // Hold the client mutex on a dedicated thread (mutex ownership is thread-affine). While it is held, a
        // connecting client must serialize behind it and so cannot reach the "check server, launch if absent" step.
        var holderThread = new Thread(() =>
        {
            using var clientMutex = new Mutex(initiallyOwned: false, DaemonPipeName.GetClientMutexName(daemon.PipeName));
            clientMutex.WaitOne();
            mutexAcquired.Set();
            releaseMutex.Wait();
            clientMutex.ReleaseMutex();
        })
        { IsBackground = true };
        holderThread.Start();
        Assert.True(mutexAcquired.Wait(TimeSpan.FromSeconds(30)));

        // The connecting client's thin client blocks on the held mutex, so it neither completes nor launches a daemon.
        var connectTask = daemon.CreateClientAsync(useNamedPipe: true);
        await Task.Delay(TimeSpan.FromSeconds(3));
        Assert.False(connectTask.IsCompleted);
        Assert.False(daemon.IsRunning);

        // Releasing the mutex lets it serialize through: it launches/connects to the daemon and initializes.
        releaseMutex.Set();
        await using var client = await connectTask;
        Assert.NotNull(client.ServerCapabilities);
        Assert.True(daemon.IsRunning);
    }

    [Fact]
    public async Task StaleDaemon_NextClientLaunchesFreshDaemon()
    {
        await using var daemon = CreateDaemon();

        await using var first = await daemon.CreateClientAsync(useNamedPipe: true);
        var firstDaemonProcess = await first.GetServerProcessAsync();
        var firstDaemonProcessId = firstDaemonProcess.Id;

        // Kill the daemon, releasing its server mutex; the next client should observe that no daemon is running.
        firstDaemonProcess.Kill(entireProcessTree: true);
        await firstDaemonProcess.WaitForExitAsync();
        Assert.True(firstDaemonProcess.HasExited);

        // With the stale daemon gone, the next client launches a brand-new daemon (a different process).
        await using var second = await daemon.CreateClientAsync(useNamedPipe: true);
        Assert.NotEqual(firstDaemonProcessId, await daemon.GetDaemonProcessIdAsync(second));
        Assert.True(daemon.IsRunning);
    }

    [Fact]
    public async Task DifferentIdentity_UsesSeparateDaemon()
    {
        // Two clients built with a different tool identity (here, a different pipe name) compute distinct daemon
        // names and so never share a daemon.
        await using var firstDaemon = CreateDaemon();
        await using var secondDaemon = CreateDaemon();
        Assert.NotEqual(firstDaemon.PipeName, secondDaemon.PipeName);

        await using var firstClient = await firstDaemon.CreateClientAsync(useNamedPipe: true);
        await using var secondClient = await secondDaemon.CreateClientAsync(useNamedPipe: true);

        Assert.NotEqual(
            await firstDaemon.GetDaemonProcessIdAsync(firstClient),
            await secondDaemon.GetDaemonProcessIdAsync(secondClient));
        Assert.True(firstDaemon.IsRunning);
        Assert.True(secondDaemon.IsRunning);
    }

    [Theory, CombinatorialData]
    public async Task TwoClients_DifferentSolutions_EachServedFromItsOwnSolution(bool useNamedPipe)
    {
        await using var daemon = CreateDaemon();

        // Two clients share the one daemon, but each opens a different single-project solution: the first solution
        // defines only the type 'Alpha', the second only 'Beta'.
        await using var first = await daemon.CreateClientAsync(useNamedPipe, workspaceContent: CreateSolutionWorkspace("First", "Alpha"));
        await using var second = await daemon.CreateClientAsync(useNamedPipe, workspaceContent: CreateSolutionWorkspace("Second", "Beta"));

        // Both clients are served by the same daemon process...
        Assert.Equal(
            await daemon.GetDaemonProcessIdAsync(first),
            await daemon.GetDaemonProcessIdAsync(second));

        // ...yet each client's hover request is answered from its own solution with no cross-contamination: the first
        // only knows 'Alpha', the second only knows 'Beta'.
        await AssertHoverDescribesTypeAsync(first, expectedType: "Alpha", otherType: "Beta");
        await AssertHoverDescribesTypeAsync(second, expectedType: "Beta", otherType: "Alpha");

        Assert.True(daemon.IsRunning);
    }

    // Test daemons use a short keepalive so that, once a test's clients disconnect, the daemon shuts itself down
    // promptly - which the TestDaemon then verifies. It is comfortably longer than the sub-second gap between a daemon
    // arming its idle timer at startup and its first client connecting, so it never fires mid-test.
    private static readonly TimeSpan s_defaultKeepAlive = TimeSpan.FromSeconds(5);

    private TestDaemon CreateDaemon(TimeSpan? keepAlive = null)
        => new(this, keepAlive ?? s_defaultKeepAlive);

    /// <summary>
    /// Connects one daemon-mode thin client to the daemon named by <paramref name="daemonPipeName"/>, opening
    /// <paramref name="workspaceContent"/> in that client's server. Clients sharing a pipe name share a daemon; distinct
    /// names use distinct daemons.
    /// </summary>
    private Task<TestLspClient> ConnectDaemonClientAsync(string daemonPipeName, bool useNamedPipe, TimeSpan keepAlive, int? initializeProcessId, LspWorkspaceContent workspaceContent)
        => CreateLanguageServerAsync(
            workspaceContent,
            new LspServerLaunchOptions
            {
                DaemonMode = true,
                DaemonPipeName = daemonPipeName,
                UseNamedPipe = useNamedPipe,
                DaemonKeepAlive = keepAlive,
                InitializeProcessId = initializeProcessId,
            });

    /// <summary>
    /// A single-project solution whose one C# file declares a type named <paramref name="typeName"/> (with that name
    /// annotated as <c>caret</c> for a hover request). Loaded via a <c>.slnx</c> file so each client opens a distinct
    /// solution rather than a bare project.
    /// </summary>
    private static LspWorkspaceContent CreateSolutionWorkspace(string projectName, string typeName)
        => LspWorkspaceContent.Empty
            .WithFile($"{projectName}.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Library</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """)
            .WithMarkupFile($"{typeName}.cs", $$"""
                public class {|caret:{{typeName}}|}
                {
                }
                """)
            .WithFile($"{projectName}.slnx", $"""
                <Solution>
                  <Project Path="{projectName}.csproj" />
                </Solution>
                """)
            .WithLoadPath($"{projectName}.slnx")
            .WithRestore();

    /// <summary>
    /// Issues a hover over the <c>caret</c>-annotated type in <paramref name="client"/>'s solution and asserts the
    /// quick info describes <paramref name="expectedType"/> and not <paramref name="otherType"/> (which belongs to the
    /// other client's solution), proving the shared daemon answered the request from this client's own solution.
    /// </summary>
    private static async Task AssertHoverDescribesTypeAsync(TestLspClient client, string expectedType, string otherType)
    {
        var caret = client.GetLocations("caret").Single();

        var hover = await client.ExecuteRequestAsync<TextDocumentPositionParams, Hover>(
            Methods.TextDocumentHoverName,
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { DocumentUri = caret.DocumentUri },
                Position = caret.Range.Start,
            },
            CancellationToken.None);

        Assert.NotNull(hover);

        // With the default (non-VS, non-markdown) client capabilities these tests use, hover content comes back as
        // plain-text MarkupContent (the fourth SumType arm).
        var hoverText = hover.Contents.Fourth.Value;
        Assert.Contains(expectedType, hoverText);
        Assert.DoesNotContain(otherType, hoverText);
    }

    /// <summary>Cleanly shuts a daemon client down (LSP shutdown/exit + close editor) and returns its thin-client exit code.</summary>
    private static async Task<int> CleanShutdownAsync(TestLspClient client)
    {
        await client.ShutdownAndExitAsync();

        // Closing our editor side lets the thin client's relay see both sides close, which it reports as a clean exit.
        client.CloseEditorTransport();
        return await WaitForThinClientExitAsync(client);
    }

    private static async Task<int> WaitForThinClientExitAsync(TestLspClient client)
    {
        await client.ThinClientProcess.WaitForExitAsync();
        return client.ThinClientProcess.ExitCode;
    }

    private static void AssertProcessAlive(int processId)
    {
        using var process = Process.GetProcessById(processId);
        Assert.False(process.HasExited);
    }

    /// <summary>
    /// A single isolated daemon for one test, identified by a unique pipe name and created via <see cref="CreateDaemon"/>.
    /// Clients are created off it with <see cref="CreateClientAsync"/> and held by the test in <c>await using</c>
    /// declarations after this daemon's, so they are disposed (disconnected) first. It tracks the daemon process(es)
    /// those clients launch and, on disposal, <em>verifies</em> that each daemon shut itself down once idle - it never
    /// kills the daemon (a daemon that fails to exit is a bug). Mirrors the in-process
    /// <c>AbstractLanguageServerHostTests.TestDaemon</c>.
    /// </summary>
    private sealed class TestDaemon(DaemonServerLifecycleTests test, TimeSpan keepAlive) : IAsyncDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<int, Process> _daemonProcessesById = [];

        /// <summary>The pipe name that scopes this daemon. Clients sharing it share a daemon.</summary>
        public string PipeName { get; } = NamedPipeTestUtilities.CreateShortPipeName("daemon-");

        /// <summary>Whether a daemon currently holds the server mutex for this pipe.</summary>
        public bool IsRunning => DaemonServerMutex.IsRunning(PipeName);

        public async Task<TestLspClient> CreateClientAsync(bool useNamedPipe, int? initializeProcessId = null, LspWorkspaceContent? workspaceContent = null)
        {
            var client = await test.ConnectDaemonClientAsync(PipeName, useNamedPipe, keepAlive, initializeProcessId, workspaceContent ?? LspWorkspaceContent.Empty);
            var daemonProcessId = (await client.GetServerProcessAsync()).Id;

            lock (_gate)
            {
                // Keep our own handle to each daemon (the first client launches it; later clients reuse it). If a stale
                // daemon is replaced, a second, distinct daemon process appears on the same pipe and is tracked too.
                if (!_daemonProcessesById.ContainsKey(daemonProcessId))
                    _daemonProcessesById[daemonProcessId] = Process.GetProcessById(daemonProcessId);
            }

            return client;
        }

        public async Task<int> GetDaemonProcessIdAsync(TestLspClient client)
            => (await client.GetServerProcessAsync()).Id;

        public async ValueTask DisposeAsync()
        {
            Process[] daemonProcesses;
            lock (_gate)
                daemonProcesses = [.. _daemonProcessesById.Values];

            // The test's clients were disposed (disconnected) first, so the daemon is now idle. Verify each daemon
            // actually shut itself down (or was already gone, e.g. a test that killed it); we never kill it ourselves.
            foreach (var daemonProcess in daemonProcesses)
            {
                using (daemonProcess)
                {
                    await daemonProcess.WaitForExitAsync();
                    Assert.True(daemonProcess.HasExited, "The daemon did not shut itself down after its last client disconnected.");
                }
            }
        }
    }
}
