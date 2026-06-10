// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // When a client needs a shared daemon it launches a second copy of this thin client as a short-lived bootstrap
        // (see DaemonClient.LaunchDaemon / DaemonBootstrap). In that mode we just launch the real daemon detached and
        // exit - we are not an editor's client, so skip normal client argument parsing and process monitoring.
        if (DaemonBootstrap.IsBootstrapRequested(args))
        {
            return await DaemonBootstrap.RunAsync(args).ConfigureAwait(false);
        }

        ThinClientArguments thinClientArguments;
        try
        {
            thinClientArguments = ThinClientArguments.Parse(args);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentNullException)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.BadArguments;
        }

        _ = StartClientProcessMonitorAsync(thinClientArguments.ClientProcessId);

        try
        {
            var executable = ServerExecutableResolver.Resolve();

            if (thinClientArguments.DaemonMode)
            {
                using var daemonResult = await DaemonClient.ConnectAsync(
                    executable,
                    thinClientArguments.ServerArguments).ConfigureAwait(false);

                if (daemonResult.DaemonConnected)
                {
                    // The daemon hosts the server for every client, so the thin client must bridge the editor's
                    // transport to the daemon's pipe (it connects to both and relays between them).
                    using var editorConnection = await EditorConnection.CreateAsync(thinClientArguments).ConfigureAwait(false);
                    return await RelayDaemonAsync(
                        daemonResult.Stream,
                        editorConnection).ConfigureAwait(false);
                }

                Console.Error.WriteLine("Running language server in non-daemon fallback mode.");
            }

            // Single-server mode: launch a dedicated server on the editor's own transport. In pipe mode the server
            // connects to the editor's pipe directly, so the thin client stays out of the LSP message path.
            return await ChildServerHost.RunAsync(
                executable,
                thinClientArguments).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException or InvalidOperationException or TimeoutException)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.ServerLaunchOrConnectFailure;
        }
    }

    private static async Task<int> RelayDaemonAsync(
        Stream daemonStream,
        EditorConnection editorConnection)
    {
        var relayResult = await LspRelay.RelayAsync(
            editorConnection.Input,
            editorConnection.Output,
            daemonStream,
            daemonStream).ConfigureAwait(false);

        // A clean LSP shutdown closes both sides; report success so the editor doesn't treat it as a crash.
        if (relayResult.BothSidesClosed)
        {
            Console.Error.WriteLine("Language server session ended cleanly.");
            return ExitCodes.Success;
        }

        if (relayResult.ClosedEndpoint == RelayEndpoint.Editor)
        {
            Console.Error.WriteLine("Editor connection closed before the language server daemon connection.");
            return ExitCodes.EditorConnectionLost;
        }

        Console.Error.WriteLine("Language server daemon connection closed before the editor connection.");
        return ExitCodes.ServerConnectionLost;
    }

    private static Task StartClientProcessMonitorAsync(int? processId)
    {
        if (processId is null)
            return Task.CompletedTask;

        return Task.Run(async () =>
        {
            Process? process = null;
            try
            {
                process = Process.GetProcessById(processId.Value);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error monitoring editor process: {ex}");
            }
            finally
            {
                Console.Error.WriteLine("Monitored editor process exited.");
                Environment.Exit(ExitCodes.EditorConnectionLost);
            }
        }, CancellationToken.None);
    }
}
