// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHostProcessManager : IAsyncDisposable
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger? _logger;
    private readonly string? _binaryLogPath;

    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly Dictionary<BuildHostProcessKind, BuildHostProcess> _processes = new();

    public BuildHostProcessManager(ILoggerFactory? loggerFactory = null, string? binaryLogPath = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<BuildHostProcessManager>();
        _binaryLogPath = binaryLogPath;
    }

    /// <summary>
    /// Returns the best <see cref="IBuildHost"/> to use for this project; if it picked a fallback option because the preferred kind was unavailable, that's returned too, otherwise null.
    /// </summary>
    public async Task<(IBuildHost, BuildHostProcessKind? PreferredKind)> GetBuildHostAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        var neededBuildHostKind = GetKindForProject(projectFilePath);
        BuildHostProcessKind? preferredKind = null;

        _logger?.LogTrace($"Choosing a build host of type {neededBuildHostKind} for {projectFilePath}.");

        if (neededBuildHostKind == BuildHostProcessKind.Mono && MonoMSBuildDiscovery.GetMonoMSBuildDirectory() == null)
        {
            _logger?.LogWarning($"An installation of Mono could not be found; {projectFilePath} will be loaded with the .NET Core SDK and may encounter errors.");
            neededBuildHostKind = BuildHostProcessKind.NetCore;
            preferredKind = BuildHostProcessKind.Mono;
        }

        var buildHost = await GetBuildHostAsync(neededBuildHostKind, cancellationToken).ConfigureAwait(false);

        // If this is a .NET Framework build host, we may not have have build tools installed and thus can't actually use it to build.
        // Check if this is the case. Unlike the mono case, we have to actually ask the other process since MSBuildLocator only allows
        // us to discover VS instances in .NET Framework hosts right now.
        if (neededBuildHostKind == BuildHostProcessKind.NetFramework)
        {
            if (!await buildHost.HasUsableMSBuildAsync(projectFilePath, cancellationToken).ConfigureAwait(false))
            {
                // It's not usable, so we'll fall back to the .NET Core one.
                _logger?.LogWarning($"An installation of Visual Studio or the Build Tools for Visual Studio could not be found; {projectFilePath} will be loaded with the .NET Core SDK and may encounter errors.");
                return (await GetBuildHostAsync(BuildHostProcessKind.NetCore, cancellationToken).ConfigureAwait(false), PreferredKind: BuildHostProcessKind.NetFramework);
            }
        }

        return (buildHost, preferredKind);
    }

    public async Task<IBuildHost> GetBuildHostAsync(BuildHostProcessKind buildHostKind, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_processes.TryGetValue(buildHostKind, out var buildHostProcess))
            {
                var processStartInfo = buildHostKind switch
                {
                    BuildHostProcessKind.NetCore => CreateDotNetCoreBuildHostStartInfo(),
                    BuildHostProcessKind.NetFramework => CreateDotNetFrameworkBuildHostStartInfo(),
                    BuildHostProcessKind.Mono => CreateMonoBuildHostStartInfo(),
                    _ => throw ExceptionUtilities.UnexpectedValue(buildHostKind)
                };

                var process = Process.Start(processStartInfo);
                Contract.ThrowIfNull(process, "Process.Start failed to launch a process.");

                buildHostProcess = new BuildHostProcess(process, _loggerFactory);
                buildHostProcess.Disconnected += BuildHostProcess_Disconnected;
                _processes.Add(buildHostKind, buildHostProcess);
            }

            return buildHostProcess.BuildHost;
        }
    }

    private void BuildHostProcess_Disconnected(object? sender, EventArgs e)
    {
        Contract.ThrowIfNull(sender, $"{nameof(BuildHostProcess)}.{nameof(BuildHostProcess.Disconnected)} was raised with a null sender.");

        Task.Run(async () =>
        {
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
            {
                processToDispose.LoggerForProcessMessages?.LogTrace("Process exited.");
                await processToDispose.DisposeAsync().ConfigureAwait(false);
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var process in _processes.Values)
            await process.DisposeAsync().ConfigureAwait(false);
    }

    private ProcessStartInfo CreateDotNetCoreBuildHostStartInfo()
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet",
        };

        // We need to roll forward to the latest runtime, since the project may be using an SDK (or an SDK required runtime) newer than we ourselves built with.
        // We set the environment variable since --roll-forward LatestMajor doesn't roll forward to prerelease SDKs otherwise.
        processStartInfo.Environment["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] = "1";
        AddArgument(processStartInfo, "--roll-forward");
        AddArgument(processStartInfo, "LatestMajor");

        var netCoreBuildHostPath = Path.Combine(Path.GetDirectoryName(typeof(BuildHostProcessManager).Assembly.Location)!, "BuildHost-net6.0", "Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll");

        AddArgument(processStartInfo, netCoreBuildHostPath);

        AppendBuildHostCommandLineArgumentsConfigureProcess(processStartInfo);

        return processStartInfo;
    }

    private ProcessStartInfo CreateDotNetFrameworkBuildHostStartInfo()
    {
        var netFrameworkBuildHost = GetPathToDotNetFrameworkBuildHost();
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = netFrameworkBuildHost,
        };

        AppendBuildHostCommandLineArgumentsConfigureProcess(processStartInfo);

        return processStartInfo;
    }

    private ProcessStartInfo CreateMonoBuildHostStartInfo()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "mono"
        };

        AddArgument(processStartInfo, GetPathToDotNetFrameworkBuildHost());

        AppendBuildHostCommandLineArgumentsConfigureProcess(processStartInfo);

        return processStartInfo;
    }

    private static string GetPathToDotNetFrameworkBuildHost()
    {
        var netFrameworkBuildHost = Path.Combine(Path.GetDirectoryName(typeof(BuildHostProcessManager).Assembly.Location)!, "BuildHost-net472", "Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe");
        Contract.ThrowIfFalse(File.Exists(netFrameworkBuildHost), $"Unable to locate the .NET Framework build host at {netFrameworkBuildHost}");
        return netFrameworkBuildHost;
    }

    private void AppendBuildHostCommandLineArgumentsConfigureProcess(ProcessStartInfo processStartInfo)
    {
        if (_binaryLogPath is not null)
        {
            AddArgument(processStartInfo, "--binlog");
            AddArgument(processStartInfo, _binaryLogPath);
        }

        // MSBUILD_EXE_PATH is read by MSBuild to find related tasks and targets. We don't want this to be inherited by our build process, or otherwise
        // it might try to load targets that aren't appropriate for the build host.
        processStartInfo.Environment.Remove("MSBUILD_EXE_PATH");

        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = false;
        processStartInfo.RedirectStandardInput = true;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
    }

    private static void AddArgument(ProcessStartInfo processStartInfo, string argument)
    {
        // On .NET Core 2.1 and higher we can just use ArgumentList to do the right thing; downlevel we need to manually
        // construct the string
#if NET
        processStartInfo.ArgumentList.Add(argument);
#else
        if (processStartInfo.Arguments.Length > 0)
            processStartInfo.Arguments += ' ';

        if (argument.Contains(' '))
            processStartInfo.Arguments += '"' + argument + '"';
        else
            processStartInfo.Arguments += argument;
#endif
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
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? BuildHostProcessKind.NetFramework : BuildHostProcessKind.Mono;
    }

    public enum BuildHostProcessKind
    {
        NetCore,
        NetFramework,
        Mono
    }

    private sealed class BuildHostProcess : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly JsonRpc _jsonRpc;

        private int _disposed = 0;

        public BuildHostProcess(Process process, ILoggerFactory? loggerFactory)
        {
            LoggerForProcessMessages = loggerFactory?.CreateLogger($"BuildHost PID {process.Id}");

            _process = process;

            _process.EnableRaisingEvents = true;
            _process.Exited += Process_Exited;

            _process.ErrorDataReceived += Process_ErrorDataReceived;

            var messageHandler = new HeaderDelimitedMessageHandler(sendingStream: _process.StandardInput.BaseStream, receivingStream: _process.StandardOutput.BaseStream, new JsonMessageFormatter());

            _jsonRpc = new JsonRpc(messageHandler);
            _jsonRpc.StartListening();
            BuildHost = _jsonRpc.Attach<IBuildHost>();

            // Call this last so our type is fully constructed before we start firing events
            _process.BeginErrorReadLine();
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is not null)
                LoggerForProcessMessages?.LogTrace($"Message from Process: {e.Data}");
        }

        public IBuildHost BuildHost { get; }
        public ILogger? LoggerForProcessMessages { get; }

        public event EventHandler? Disconnected;

        public async ValueTask DisposeAsync()
        {
            // Ensure only one thing disposes; while we disconnect the process will go away, which will call us to do this again
            if (Interlocked.CompareExchange(ref _disposed, value: 1, comparand: 0) != 0)
                return;

            // We will call Shutdown in a try/catch; if the process has gone bad it's possible the connection is no longer functioning.
            try
            {
                await BuildHost.ShutdownAsync().ConfigureAwait(false);
                _jsonRpc.Dispose();

                LoggerForProcessMessages?.LogTrace("Process gracefully shut down.");
            }
            catch (Exception e)
            {
                LoggerForProcessMessages?.LogError(e, "Exception while shutting down the BuildHost process.");

                // OK, process may have gone bad.
                _process.Kill();
            }
        }
    }
}

