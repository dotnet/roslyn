// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class BuildHostProcessManager : IAsyncDisposable
{
    private readonly string? _binaryLogPath;

    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly Dictionary<BuildHostProcessKind, BuildHostProcess> _processes = new();

    public BuildHostProcessManager(string? binaryLogPath = null)
    {
        _binaryLogPath = binaryLogPath;
    }

    public async Task<IBuildHost> GetBuildHostAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        var neededBuildHostKind = GetKindForProject(projectFilePath);

        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_processes.TryGetValue(neededBuildHostKind, out var buildHostProcess))
            {
                var process = neededBuildHostKind switch
                {
                    BuildHostProcessKind.NetCore => LaunchDotNetCoreBuildHost(),
                    BuildHostProcessKind.NetFramework => LaunchDotNetFrameworkBuildHost(),
                    _ => throw ExceptionUtilities.UnexpectedValue(neededBuildHostKind)
                };

                buildHostProcess = new BuildHostProcess(process);
                buildHostProcess.Disconnected += BuildHostProcess_Disconnected;
                _processes.Add(neededBuildHostKind, buildHostProcess);
            }

            return buildHostProcess.BuildHost;
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods: We're responding to Process.Exited, so an async void event handler is all we can do
    private async void BuildHostProcess_Disconnected(object? sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        Contract.ThrowIfNull(sender, $"{nameof(BuildHostProcess)}.{nameof(BuildHostProcess.Disconnected)} was raised with a null sender.");

        BuildHostProcess? processToDispose = null;

        using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
        {
            // Remove it from our map; it's possible it might have already been removed if we had more than one way we observed a disconnect.
            var existingProcess = _processes.SingleOrNull(p => p.Value == sender);
            if (existingProcess.HasValue)
            {
                processToDispose = existingProcess.Value.Value;
                _processes.Remove(existingProcess.Value.Key);
            }
        }

        // Dispose outside of the lock (even though we don't expect much to happen at this point)
        if (processToDispose != null)
            await processToDispose.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var process in _processes.Values)
            await process.DisposeAsync();
    }

    private Process LaunchDotNetCoreBuildHost()
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
        processStartInfo.Environment["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] = "1";
        processStartInfo.ArgumentList.Add("--roll-forward");
        processStartInfo.ArgumentList.Add("LatestMajor");

        processStartInfo.ArgumentList.Add(typeof(IBuildHost).Assembly.Location);

        AppendBuildHostCommandLineArguments(processStartInfo);

        var process = Process.Start(processStartInfo);
        Contract.ThrowIfNull(process, "Process.Start failed to launch a process.");
        return process;
    }

    private Process LaunchDotNetFrameworkBuildHost()
    {
        var netFrameworkBuildHost = Path.Combine(Path.GetDirectoryName(typeof(BuildHostProcessManager).Assembly.Location)!, "BuildHost-net472", "Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe");
        Contract.ThrowIfFalse(File.Exists(netFrameworkBuildHost), $"Unable to locate the .NET Framework build host at {netFrameworkBuildHost}");

        var processStartInfo = new ProcessStartInfo()
        {
            FileName = netFrameworkBuildHost,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        };

        AppendBuildHostCommandLineArguments(processStartInfo);

        var process = Process.Start(processStartInfo);
        Contract.ThrowIfNull(process, "Process.Start failed to launch a process.");
        return process;
    }

    private void AppendBuildHostCommandLineArguments(ProcessStartInfo processStartInfo)
    {
        if (_binaryLogPath is not null)
        {
            processStartInfo.ArgumentList.Add("--binlog");
            processStartInfo.ArgumentList.Add(_binaryLogPath);
        }
    }

    private static readonly XmlReaderSettings s_xmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
    };

    private static BuildHostProcessKind GetKindForProject(string projectFilePath)
    {
        // This implements the algorithm as stated in https://github.com/dotnet/project-system/blob/9a761848e0f330a45e349685a266fea00ac3d9c5/docs/opening-with-new-project-system.md;
        // we'll load the XML of the project directly, and inspect for certain elements.
        XDocument document;

        // Read the XML, prohibiting DTD processing due the the usual concerns there.
        using (var fileStream = new FileStream(projectFilePath, FileMode.Open, FileAccess.Read))
        using (var xmlReader = XmlReader.Create(fileStream, s_xmlSettings))
            document = XDocument.Load(xmlReader);

        // If we don't have a root, doesn't really matter which. This project is just malformed.
        if (document.Root == null)
            return BuildHostProcessKind.NetCore;

        // Look for SDK attribute on the root
        if (document.Root.Attribute("Sdk") != null)
            return BuildHostProcessKind.NetCore;

        // Look for <Import Sdk=... />
        if (document.Root.Elements("Import").Attributes("Sdk").Any())
            return BuildHostProcessKind.NetCore;

        // Look for <Sdk ... />
        if (document.Root.Elements("Sdk").Any())
            return BuildHostProcessKind.NetCore;

        // Looking for PropertyGroups that contain TargetFramework or TargetFrameworks nodes
        var propertyGroups = document.Descendants("PropertyGroup");
        if (propertyGroups.Elements("TargetFramework").Any() || propertyGroups.Elements("TargetFrameworks").Any())
            return BuildHostProcessKind.NetCore;

        // Nothing that indicates it's an SDK-style project, so use our .NET framework host
        return BuildHostProcessKind.NetFramework;
    }

    private enum BuildHostProcessKind
    {
        NetCore,
        NetFramework
    }

    private sealed class BuildHostProcess : IAsyncDisposable
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

        public async ValueTask DisposeAsync()
        {
            // We will call Shutdown in a try/catch; if the process has gone bad it's possible the connection is no longer functioning.
            try
            {
                await BuildHost.ShutdownAsync();
            }
            catch (Exception)
            {
                // OK, process may have gone bad.
                _process.Kill();
            }

            _jsonRpc.Dispose();
        }
    }
}

