// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

public partial class AbstractLanguageServerClientTests
{
    /// <summary>
    /// Starts a long-lived, killable process to stand in for the editor in scenarios where the test needs to kill the
    /// monitored client process (the test process itself can't be killed). Only its liveness and process id matter.
    /// </summary>
    private protected static IdleProcess StartIdleEditorProcess()
    {
        var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo("cmd.exe")
            : new ProcessStartInfo("sleep");

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("pause > nul");
        }
        else
        {
            startInfo.ArgumentList.Add("2147483647");
        }

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the idle editor process.");

        // Drain output so the process never blocks on a full pipe; the content is irrelevant.
        process.OutputDataReceived += static (_, _) => { };
        process.ErrorDataReceived += static (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return new IdleProcess(process);
    }

    private protected sealed class IdleProcess(Process process) : IDisposable
    {
        public int Id { get; } = process.Id;

        public void Kill()
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }

        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            process.Dispose();
        }
    }
}
