// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class BuildHostProcessManager : IDisposable
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private BuildHostProcess? _process;

    public async Task<IBuildHost> GetBuildHostAsync(CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_process == null)
            {
                _process = new BuildHostProcess(LaunchDotNetCoreBuildHost());
                _process.Disconnected += BuildHostProcess_Disconnected;
            }

            return _process.BuildHost;
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods: We're responding to Process.Exited, so an async void event handler is all we can do
    private async void BuildHostProcess_Disconnected(object? sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        Contract.ThrowIfNull(sender, $"{nameof(BuildHostProcess)}.{nameof(BuildHostProcess.Disconnected)} was raised with a null sender.");

        using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
        {
            if (_process == sender)
            {
                _process.Dispose();
                _process = null;
            }
        }
    }

    private static Process LaunchDotNetCoreBuildHost()
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        };

        // We need to roll forward to the latest runtime, since the project may be using an SDK (or an SDK required runtime) newer than we ourselves built with.
        // We set the environment variable since --roll-forward LatestMajor doesn't roll forward to prerelease SDKs otherwise.
        processStartInfo.ArgumentList.Add("--roll-forward");
        processStartInfo.ArgumentList.Add("LatestMajor");
        processStartInfo.Environment["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] = "1";

        processStartInfo.ArgumentList.Add(typeof(IBuildHost).Assembly.Location);
        var process = Process.Start(processStartInfo);
        Contract.ThrowIfNull(process, "Process.Start failed to launch a process.");
        return process;
    }

    public void Dispose()
    {
        _process?.Dispose();
    }

    private sealed class BuildHostProcess : IDisposable
    {
        private readonly Process _process;
        private readonly JsonRpc _jsonRpc;

        public BuildHostProcess(Process process)
        {
            _process = process;

            _process.EnableRaisingEvents = true;
            _process.Exited += Process_Exited;

            var messageHandler = new HeaderDelimitedMessageHandler(sendingStream: _process.StandardInput.BaseStream, receivingStream: _process.StandardOutput.BaseStream, new JsonMessageFormatter());

            _jsonRpc = new JsonRpc(messageHandler);
            _jsonRpc.StartListening();
            BuildHost = _jsonRpc.Attach<IBuildHost>();
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public IBuildHost BuildHost { get; }

        public event EventHandler? Disconnected;

        public void Dispose()
        {
            _jsonRpc.Dispose();
        }
    }
}

