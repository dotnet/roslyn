// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

/// <summary>
/// Single-server (non-daemon) mode: launches the bundled language server as a dedicated child process on the same
/// transport the editor asked the thin client to use, and always forwards the server's stdio to/from the thin
/// client's own. The transport only changes which argument the server is launched with:
/// <list type="bullet">
/// <item>Named pipe (<c>--pipe &lt;name&gt;</c>): the server connects to the editor's pipe directly for LSP, so the
/// forwarded stdio carries only the server's diagnostics.</item>
/// <item>Stdio (<c>--stdio</c>): the server uses stdio for LSP, so the forwarded stdin/stdout carry the LSP
/// requests/responses (and stderr still carries diagnostics).</item>
/// </list>
/// The editor's <c>--clientProcessId</c> is also forwarded so the dedicated server independently monitors the editor
/// and exits on its own when the editor goes away (the thin client monitors it too). In both cases the server process
/// exiting is the authoritative end-of-session signal.
/// </summary>
internal static class ChildServerHost
{
    public static async Task<int> RunAsync(
        ServerExecutable executable,
        ThinClientArguments arguments)
    {
        // Forward the editor's transport to the server: in pipe mode it connects to the editor's pipe directly; in
        // stdio mode it uses the stdio we forward below.
        var childArguments = new List<string>(arguments.ServerArguments.Length + 4);
        if (arguments.EditorTransportKind == EditorTransportKind.Pipe)
        {
            childArguments.Add("--pipe");
            childArguments.Add(arguments.EditorPipeName!);
        }
        else
        {
            childArguments.Add("--stdio");
        }

        // Forward the editor's process id so this dedicated server monitors the editor and exits on its own when the
        // editor goes away. (Not forwarded to a shared daemon, which must outlive any single client.)
        if (arguments.ClientProcessId is int clientProcessId)
        {
            childArguments.Add("--clientProcessId");
            childArguments.Add(clientProcessId.ToString(CultureInfo.InvariantCulture));
        }

        childArguments.AddRange(arguments.ServerArguments);

        using var process = StartChildProcess(executable, childArguments);
        using var forwardingCancellationSource = new CancellationTokenSource();

        // Always bridge the server's stdio to ours; the server decides whether to use it for LSP (stdio transport)
        // or the editor's pipe (pipe transport). Our stdin -> the server's stdin carries the editor's LSP requests
        // in stdio mode; the server's stdout/stderr -> ours carries LSP responses (stdio mode) and diagnostics.
        _ = ForwardEditorInputAsync(process, forwardingCancellationSource.Token);
        var stdoutTask = ProcessUtilities.ForwardStreamAsync(process.StandardOutput.BaseStream, Console.OpenStandardOutput(), CancellationToken.None);
        var stderrTask = ProcessUtilities.ForwardStreamAsync(process.StandardError.BaseStream, Console.OpenStandardError(), CancellationToken.None);

        Console.Error.WriteLine($"Started language server child: {ProcessUtilities.GetCommandLineForDisplay(executable, childArguments)}");

        await process.WaitForExitAsync().ConfigureAwait(false);

        // The server has exited; stop forwarding and let the output forwarders drain what's left.
        forwardingCancellationSource.Cancel();
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        return InterpretExit(process);
    }

    /// <summary>
    /// Forwards the editor's input (our stdin) to the server's stdin, then closes the server's stdin once ours ends
    /// so a stdio-mode server sees EOF and shuts down. A pipe-mode server ignores its stdin, so this is harmless there.
    /// </summary>
    private static async Task ForwardEditorInputAsync(Process process, CancellationToken cancellationToken)
    {
        await ProcessUtilities.ForwardStreamAsync(Console.OpenStandardInput(), process.StandardInput.BaseStream, cancellationToken).ConfigureAwait(false);

        try
        {
            process.StandardInput.Close();
        }
        catch (IOException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static int InterpretExit(Process process)
    {
        // A clean child exit (code 0) means the session shut down gracefully (e.g. the editor sent `exit`,
        // or the server shut itself down). Surface success so the editor doesn't treat it as a crash.
        if (process.HasExited && process.ExitCode == ExitCodes.Success)
        {
            Console.Error.WriteLine("Language server child exited cleanly.");
            return ExitCodes.Success;
        }

        if (process.HasExited && process.ExitCode != ExitCodes.Success)
        {
            Console.Error.WriteLine($"Language server child exited with code {process.ExitCode}.");
            return process.ExitCode;
        }

        Console.Error.WriteLine("Language server child did not exit as expected.");
        return ExitCodes.ServerConnectionLost;
    }

    private static Process StartChildProcess(ServerExecutable executable, IReadOnlyList<string> childArguments)
    {
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        executable.ConfigureStartInfo(startInfo);
        foreach (var argument in childArguments)
            startInfo.ArgumentList.Add(argument);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the language server child process.");
    }
}
