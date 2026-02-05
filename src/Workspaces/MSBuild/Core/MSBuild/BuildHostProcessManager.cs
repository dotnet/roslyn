// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHostProcessManager : IAsyncDisposable
{
    private readonly ImmutableDictionary<string, string> _globalMSBuildProperties;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger? _logger;
    private readonly IBinLogPathProvider? _binaryLogPathProvider;

    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly Dictionary<BuildHostProcessKind, BuildHostProcess> _processes = [];

    private static string MSBuildWorkspaceDirectory => Path.GetDirectoryName(typeof(BuildHostProcessManager).Assembly.Location) ?? AppContext.BaseDirectory;
    private static bool IsLoadedFromNuGetPackage => File.Exists(Path.Combine(MSBuildWorkspaceDirectory, "..", "..", "microsoft.codeanalysis.workspaces.msbuild.nuspec"));

    private static readonly string DotnetExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

    public BuildHostProcessManager(ImmutableDictionary<string, string>? globalMSBuildProperties = null, IBinLogPathProvider? binaryLogPathProvider = null, ILoggerFactory? loggerFactory = null)
    {
        _globalMSBuildProperties = globalMSBuildProperties ?? ImmutableDictionary<string, string>.Empty;
        _binaryLogPathProvider = binaryLogPathProvider;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<BuildHostProcessManager>();
    }

    /// <summary>
    /// Determines the proper host for processing a project, and returns a <see cref="RemoteBuildHost"/> to service questions about that project; if a proper build host
    /// cannot be created for it, another build host is returned to attempt to get useful results.
    /// </summary>
    public async Task<RemoteBuildHost> GetBuildHostWithFallbackAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        var (buildHost, _) = await GetBuildHostWithFallbackAsync(GetKindForProject(projectFilePath), projectFilePath, cancellationToken).ConfigureAwait(false);
        return buildHost;
    }

    /// <summary>
    /// Returns the type of build host requested, falling back to another type of build host if it cannot be created. The actual kind of the other build host is returned; the caller can
    /// check if the kinds differ to know if a fallback was performed.
    /// </summary>
    public async Task<(RemoteBuildHost buildHost, BuildHostProcessKind actualKind)> GetBuildHostWithFallbackAsync(BuildHostProcessKind buildHostKind, string projectOrSolutionFilePath, CancellationToken cancellationToken)
    {
        if (buildHostKind == BuildHostProcessKind.Mono && MonoMSBuildDiscovery.GetMonoMSBuildVersion() == null)
        {
            _logger?.LogWarning($"An installation of Mono MSBuild could not be found; {projectOrSolutionFilePath} will be loaded with the .NET Core SDK and may encounter errors.");
            buildHostKind = BuildHostProcessKind.NetCore;
        }

        var buildHost = await GetBuildHostAsync(buildHostKind, projectOrSolutionFilePath, dotnetPath: null, cancellationToken).ConfigureAwait(false);

        // If this is a .NET Framework build host, we may not have have build tools installed and thus can't actually use it to build.
        // Check if this is the case. Unlike the mono case, we have to actually ask the other process since MSBuildLocator only allows
        // us to discover VS instances in .NET Framework hosts right now.
        if (buildHostKind == BuildHostProcessKind.NetFramework)
        {
            if (!await buildHost.HasUsableMSBuildAsync(projectOrSolutionFilePath, cancellationToken).ConfigureAwait(false))
            {
                // It's not usable, so we'll fall back to the .NET Core one.
                _logger?.LogWarning($"An installation of Visual Studio or the Build Tools for Visual Studio could not be found; {projectOrSolutionFilePath} will be loaded with the .NET Core SDK and may encounter errors.");
                return (await GetBuildHostAsync(BuildHostProcessKind.NetCore, projectOrSolutionFilePath, dotnetPath: null, cancellationToken).ConfigureAwait(false), BuildHostProcessKind.NetCore);
            }
        }

        return (buildHost, buildHostKind);
    }

    public Task<RemoteBuildHost> GetBuildHostAsync(BuildHostProcessKind buildHostKind, CancellationToken cancellationToken)
    {
        return GetBuildHostAsync(buildHostKind, projectOrSolutionFilePath: null, dotnetPath: null, cancellationToken);
    }

    public async Task<RemoteBuildHost> GetBuildHostAsync(BuildHostProcessKind buildHostKind, string? projectOrSolutionFilePath, string? dotnetPath, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_processes.TryGetValue(buildHostKind, out var buildHostProcess))
            {
                buildHostProcess = await NoLock_GetBuildHostAsync(buildHostKind, projectOrSolutionFilePath, dotnetPath, cancellationToken).ConfigureAwait(false);

                _processes.Add(buildHostKind, buildHostProcess);
            }

            return buildHostProcess.BuildHost;
        }

        async Task<BuildHostProcess> NoLock_GetBuildHostAsync(BuildHostProcessKind buildHostKind, string? projectOrSolutionFilePath, string? dotnetPath, CancellationToken cancellationToken)
        {
            var pipeName = Guid.NewGuid().ToString();
            var processStartInfo = CreateBuildHostStartInfo(buildHostKind, pipeName, dotnetPath);

            var process = Process.Start(processStartInfo);
            Contract.ThrowIfNull(process, "Process.Start failed to launch a process.");

            var buildHostProcess = new BuildHostProcess(process, _loggerFactory);
            buildHostProcess.Disconnected += BuildHostProcess_Disconnected;

            try
            {
                await buildHostProcess.ConnectAsync(pipeName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // We failed to connect to the process, kill it if it's still around
                if (!process.HasExited)
                    process.Kill();

                buildHostProcess.LogProcessFailure();
                throw new Exception($"The build host was started but we were unable to connect to it's pipe. The process exited with {process.ExitCode}. Process output:{Environment.NewLine}{buildHostProcess.GetBuildHostProcessOutput()}", innerException: e);
            }

            await buildHostProcess.BuildHost.ConfigureGlobalStateAsync(new Dictionary<string, string>(_globalMSBuildProperties), _binaryLogPathProvider?.GetNewLogPath(), cancellationToken).ConfigureAwait(false);

            if (buildHostKind != BuildHostProcessKind.NetCore
                || projectOrSolutionFilePath is null
                || dotnetPath is not null)
            {
                return buildHostProcess;
            }

            // When running on .NET Core, we need to find the right SDK location that can load our project and restart the BuildHost if required.
            // When dotnetPath is null, the BuildHost is started with the default dotnet executable, which may not be the right one for the project.

            var processPath = GetProcessPath();

            // The running BuildHost will be able to search through all the SDK install locations for a usable MSBuild instance.
            var msbuildLocation = await buildHostProcess.BuildHost.FindBestMSBuildAsync(projectOrSolutionFilePath, cancellationToken).ConfigureAwait(false);
            if (msbuildLocation is null)
            {
                return buildHostProcess;
            }

            // The layout of the SDK is such that the dotnet executable is always at the same relative path from the MSBuild location.
            dotnetPath = Path.GetFullPath(Path.Combine(msbuildLocation.Path, $"../../{DotnetExecutable}"));

            // If the dotnetPath is null or the file doesn't exist, we can't do anything about it; the BuildHost will just use the default dotnet executable.
            // If the dotnetPath is the same as processPath then we are already running from the right dotnet executable, so we don't need to relaunch.
            if (dotnetPath is null || processPath == dotnetPath || !File.Exists(dotnetPath))
            {
                return buildHostProcess;
            }

            // We need to relaunch the .NET BuildHost from a different dotnet instance.
            buildHostProcess.Disconnected -= BuildHostProcess_Disconnected;
            await buildHostProcess.DisposeAsync().ConfigureAwait(false);
            _logger?.LogInformation(".NET BuildHost started from {ProcessPath} reloading to start from {DotnetPath} to match necessary SDK location.", processPath, dotnetPath);

            return await NoLock_GetBuildHostAsync(buildHostKind, projectOrSolutionFilePath, dotnetPath, cancellationToken).ConfigureAwait(false);
        }

#if NET
        static string GetProcessPath() => Environment.ProcessPath ?? throw new InvalidOperationException("Unable to determine the path of the current process.");
#else
        static string GetProcessPath() => Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Unable to determine the path of the current process.");
#endif
    }

    internal static ProcessStartInfo CreateBuildHostStartInfo(BuildHostProcessKind buildHostKind, string pipeName, string? dotnetPath)
    {
        return buildHostKind switch
        {
            BuildHostProcessKind.NetCore => CreateDotNetCoreBuildHostStartInfo(pipeName, dotnetPath),
            BuildHostProcessKind.NetFramework => CreateDotNetFrameworkBuildHostStartInfo(pipeName),
            BuildHostProcessKind.Mono => CreateMonoBuildHostStartInfo(pipeName),
            _ => throw ExceptionUtilities.UnexpectedValue(buildHostKind)
        };
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
                processToDispose.LogProcessFailure();
                await processToDispose.DisposeAsync().ConfigureAwait(false);
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        List<BuildHostProcess> processesToDispose;

        // Copy the list out while we're in the lock, otherwise as we dispose these events will get fired, which
        // may try to mutate the list while we're enumerating.
        using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
        {
            processesToDispose = [.. _processes.Values];
            _processes.Clear();
        }

        foreach (var process in processesToDispose)
            await process.DisposeAsync().ConfigureAwait(false);
    }

    private static ProcessStartInfo CreateDotNetCoreBuildHostStartInfo(string pipeName, string? dotnetPath)
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = dotnetPath ?? DotnetExecutable,
        };

        // We need to roll forward to the latest runtime, since the project may be using an SDK (or an SDK required runtime) newer than we ourselves built with.
        // We set the environment variable since --roll-forward LatestMajor doesn't roll forward to prerelease SDKs otherwise.
        processStartInfo.Environment["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] = "1";
        AddArgument(processStartInfo, "--roll-forward");
        AddArgument(processStartInfo, "LatestMajor");

        var netCoreBuildHostPath = GetNetCoreBuildHostPath();

        AddArgument(processStartInfo, netCoreBuildHostPath);

        AppendBuildHostCommandLineArgumentsAndConfigureProcess(processStartInfo, pipeName);

        return processStartInfo;
    }

    internal static string GetNetCoreBuildHostPath()
    {
        return GetBuildHostPath("BuildHost-netcore", "Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll");
    }

    private static ProcessStartInfo CreateDotNetFrameworkBuildHostStartInfo(string pipeName)
    {
        var netFrameworkBuildHost = GetDotNetFrameworkBuildHostPath();
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = netFrameworkBuildHost,
        };

        AppendBuildHostCommandLineArgumentsAndConfigureProcess(processStartInfo, pipeName);

        return processStartInfo;
    }

    private static ProcessStartInfo CreateMonoBuildHostStartInfo(string pipeName)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "mono"
        };

        AddArgument(processStartInfo, GetDotNetFrameworkBuildHostPath());

        AppendBuildHostCommandLineArgumentsAndConfigureProcess(processStartInfo, pipeName);

        return processStartInfo;
    }

    private static string GetDotNetFrameworkBuildHostPath()
    {
        return GetBuildHostPath("BuildHost-net472", "Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe");
    }

    private static string GetBuildHostPath(string contentFolderName, string assemblyName)
    {
        // Possible BuildHost paths are relative to where the Workspaces.MSBuild assembly was loaded.
        string buildHostPath;

        if (IsLoadedFromNuGetPackage)
        {
            // When Workspaces.MSBuild is loaded from the NuGet package (as is the case in .NET Interactive, NCrunch, and possibly other use cases)
            // the Build host is deployed under the contentFiles folder.
            //
            // Workspaces.MSBuild.dll Path - .nuget/packages/microsoft.codeanalysis.workspaces.msbuild/{version}/lib/{tfm}/Microsoft.CodeAnalysis.Workspaces.MSBuild.dll
            // MSBuild.BuildHost.dll Path  - .nuget/packages/microsoft.codeanalysis.workspaces.msbuild/{version}/contentFiles/any/any/{contentFolderName}/{assemblyName}

            buildHostPath = Path.GetFullPath(Path.Combine(MSBuildWorkspaceDirectory, "..", "..", "contentFiles", "any", "any", contentFolderName, assemblyName));
        }
        else
        {
            // When Workspaces.MSBuild is deployed as part of an application the build host is deployed as a content folder next to the application.
            buildHostPath = Path.Combine(MSBuildWorkspaceDirectory, contentFolderName, assemblyName);
        }

        if (!File.Exists(buildHostPath))
            throw new Exception(string.Format(WorkspaceMSBuildResources.The_build_host_could_not_be_found_at_0, buildHostPath));

        return buildHostPath;
    }

    private static void AppendBuildHostCommandLineArgumentsAndConfigureProcess(ProcessStartInfo processStartInfo, string pipeName)
    {
        AddArgument(processStartInfo, pipeName);
        AddArgument(processStartInfo, System.Globalization.CultureInfo.CurrentUICulture.Name);

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

    public static BuildHostProcessKind GetKindForProject(string projectFilePath)
    {
        // In Source Build builds, we don't have a net472 host at all, so the answer is simple. We unfortunately can't create a net472 build because there's no
        // reference assemblies to build against.
#if DOTNET_BUILD_FROM_SOURCE
        return BuildHostProcessKind.NetCore;
#else

        // This implements the algorithm as stated in https://github.com/dotnet/project-system/blob/9a761848e0f330a45e349685a266fea00ac3d9c5/docs/opening-with-new-project-system.md;
        // we'll load the XML of the project directly, and inspect for certain elements.
        XDocument document;

        var frameworkHostType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? BuildHostProcessKind.NetFramework : BuildHostProcessKind.Mono;

        try
        {
            // Read the XML, prohibiting DTD processing due the the usual security concerns there.
            using var fileStream = FileUtilities.OpenRead(projectFilePath);
            using var xmlReader = XmlReader.Create(fileStream, s_xmlSettings);
            document = XDocument.Load(xmlReader);
        }
        catch (Exception e) when (e is IOException or XmlException)
        {
            // We were unable to read the file; rather than having callers of the build process manager have to deal with this special case
            // we'll instead just give them a host that corresponds to what they are running as; we know that host unquestionably exists
            // and the rest of the code can deal with this cleanly.
#if NET
            return BuildHostProcessKind.NetCore;
#else
            return frameworkHostType;
#endif
        }

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
        return frameworkHostType;

#endif
    }

    private sealed class BuildHostProcess : IAsyncDisposable
    {
        private readonly ILogger? _logger;
        private readonly Process _process;
        private RpcClient? _rpcClient;
        private RemoteBuildHost? _buildHost;

        /// <summary>
        /// A string builder where we collect the process log messages, in case we do want to know them if the process crashes.
        /// Reads/writes should be synchronized by locking this object.
        /// </summary>
        private readonly StringBuilder _processLogMessages = new();

        private int _disposed = 0;

        public BuildHostProcess(Process process, ILoggerFactory? loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger($"BuildHost PID {process.Id}");
            _process = process;

            _process.EnableRaisingEvents = true;
            _process.Exited += Process_Exited;

            // Hook up event handlers to see stdout/stderr from the process, and then call Begin*ReadLine to start getting events
            _process.OutputDataReceived += (_, e) => LogProcessOutput(e, "stdout");
            _process.ErrorDataReceived += (_, e) => LogProcessOutput(e, "stderr");
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // Close the standard input stream so that if any build tasks were to try reading from the console, they won't deadlock waiting for input.
            _process.StandardInput.Close();
        }

        public async Task ConnectAsync(string pipeName)
        {
            var pipeClient = NamedPipeUtil.CreateClient(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync(timeout: 60_000).ConfigureAwait(false);

            if (!NamedPipeUtil.CheckPipeConnectionOwnership(pipeClient))
            {
                throw new Exception("Ownership of BuildHost pipe is incorrect.");
            }

            _rpcClient = new RpcClient(pipeClient);
            _rpcClient.Start();
            _rpcClient.Disconnected += Process_Exited;
            _buildHost = new RemoteBuildHost(_rpcClient);
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private void LogProcessOutput(DataReceivedEventArgs e, string outputName)
        {
            if (e.Data is not null)
            {
                lock (_processLogMessages)
                    _processLogMessages.AppendLine(e.Data);

                _logger?.LogTrace($"Message on {outputName}: {e.Data}");
            }
        }

        public RemoteBuildHost BuildHost => _buildHost ?? throw new InvalidOperationException("Build host is not connected.");

        public event EventHandler? Disconnected;

        public async ValueTask DisposeAsync()
        {
            // Ensure only one thing disposes; while we disconnect the process will go away, which will call us to do this again
            if (Interlocked.CompareExchange(ref _disposed, value: 1, comparand: 0) != 0)
                return;

            // We will call Shutdown in a try/catch; if the process has gone bad it's possible the connection is no longer functioning.
            try
            {
                if (!_process.HasExited)
                {
                    _logger?.LogTrace("Sending a Shutdown request to the BuildHost.");

                    await BuildHost.ShutdownAsync(CancellationToken.None).ConfigureAwait(false);
                }

                if (_rpcClient is not null)
                {
                    _rpcClient.Shutdown();
                    _logger?.LogTrace("Process shut down.");
                }
                else
                {
                    // We never successfully connected to the process, so just kill it
                    _process.Kill();
                    _logger?.LogTrace("Process killed since it was never connected");
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception while shutting down the BuildHost process.");

                // Process may have gone bad, so not much else we can do.
                LogProcessFailure();
                _process.Kill();
            }
        }

        public void LogProcessFailure()
        {
            if (_logger == null)
                return;

            var processLog = GetBuildHostProcessOutput();

            if (!_process.HasExited)
                _logger.LogError("The BuildHost process is not responding. Process output:{newLine}{processLog}", Environment.NewLine, processLog);
            else if (_process.ExitCode != 0)
                _logger.LogError("The BuildHost process exited with {errorCode}. Process output:{newLine}{processLog}", _process.ExitCode, Environment.NewLine, processLog);
        }

        public string GetBuildHostProcessOutput()
        {
            lock (_processLogMessages)
                return _processLogMessages.ToString();
        }
    }
}
