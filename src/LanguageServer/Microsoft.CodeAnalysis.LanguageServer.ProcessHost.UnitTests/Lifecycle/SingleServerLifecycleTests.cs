// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

/// <summary>
/// End-to-end lifecycle tests for the single-server (non-daemon) language server. Each test drives the real process
/// chain (test editor -> thin client -> dedicated server) over both the stdio and named-pipe transports, killing
/// individual processes to verify the rest of the stack reacts as documented and that exit codes map to the documented
/// scheme.
/// </summary>
public sealed class SingleServerLifecycleTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    private Task<TestLspClient> StartAsync(bool useNamedPipe, int? clientProcessId = null)
        => CreateLanguageServerAsync(
            LspWorkspaceContent.Empty,
            new LspServerLaunchOptions { UseNamedPipe = useNamedPipe, ClientProcessId = clientProcessId });

    [Theory, CombinatorialData]
    public async Task StartsAndShutsDownCleanly(bool useNamedPipe)
    {
        await using var client = await StartAsync(useNamedPipe);

        await client.ShutdownAndExitAsync();

        var exitCode = await WaitForThinClientExitAsync(client);
        Assert.Equal(0, exitCode);

        await AssertServerProcessExitedAsync(client);
    }

    [Theory, CombinatorialData]
    public async Task EditorKilled_ThinClientAndServerExit(bool useNamedPipe)
    {
        // The test process can't kill itself, so stand up a separate, killable process to play the editor that the
        // thin client and server are asked to monitor via --clientProcessId.
        using var editor = StartIdleEditorProcess();

        await using var client = await StartAsync(useNamedPipe, editor.Id);

        editor.Kill();

        // Both the dedicated server (self-monitoring the editor) and the thin client should tear down, with the thin
        // client reporting an editor-disconnect exit code.
        await AssertServerProcessExitedAsync(client);

        var exitCode = await WaitForThinClientExitAsync(client);
        Assert.NotEqual(0, exitCode);
    }

    [Theory, CombinatorialData]
    public async Task ThinClientKilled_ServerExits(bool useNamedPipe)
    {
        await using var client = await StartAsync(useNamedPipe);

        client.ThinClientProcess.Kill();

        if (useNamedPipe)
        {
            // In pipe mode the server talks to the editor's pipe directly, so killing only the thin client doesn't
            // break the server's LSP transport (the editor pipe and the monitored editor process both stay alive). A
            // real editor reacts to its spawned language-server tool dying by dropping the pipe; emulate that so the
            // server observes the disconnect and exits.
            client.CloseEditorTransport();
        }

        await AssertServerProcessExitedAsync(client);
    }

    [Theory, CombinatorialData]
    public async Task ServerKilled_ThinClientExitsNonZero(bool useNamedPipe)
    {
        await using var client = await StartAsync(useNamedPipe);
        var serverProcess = await client.GetServerProcessAsync();

        serverProcess.Kill(entireProcessTree: true);

        var exitCode = await WaitForThinClientExitAsync(client);

        Assert.NotEqual(0, exitCode);
    }

    private static async Task<int> WaitForThinClientExitAsync(TestLspClient client)
    {
        var thinClientProcess = client.ThinClientProcess;
        await thinClientProcess.WaitForExitAsync();
        return thinClientProcess.ExitCode;
    }

    private static async Task AssertServerProcessExitedAsync(TestLspClient client)
    {
        var serverProcess = await client.GetServerProcessAsync();
        await serverProcess.WaitForExitAsync();
        Assert.True(serverProcess.HasExited);
    }
}
