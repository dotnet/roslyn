// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHost : IBuildHost
{
    private readonly BuildHostLogger _logger;
    private readonly RpcServer _server;
    private readonly object _gate = new();
    private ProjectBuildManager? _buildManager;

    /// <summary>
    /// The global properties to use for all builds; should not be changed once the <see cref="_buildManager"/> is initialized.
    /// </summary>
    private Dictionary<string, string>? _globalMSBuildProperties;

    /// <summary>
    /// The binary log path to use for all builds; should not be changed once the <see cref="_buildManager"/> is initialized.
    /// </summary>
    private string? _binaryLogPath;

    public BuildHost(BuildHostLogger logger, RpcServer server)
    {
        _logger = logger;
        _server = server;
    }

    private bool TryEnsureMSBuildLoaded(string projectOrSolutionFilePath)
    {
        lock (_gate)
        {
            // If we've already created our MSBuild types, then there's nothing further to do.
            if (MSBuildLocator.IsRegistered)
            {
                return true;
            }

            var instance = FindMSBuild(projectOrSolutionFilePath, includeUnloadableInstances: false);
            if (instance is null)
            {
                return false;
            }

            MSBuildLocator.RegisterMSBuildPath(instance.Path);
            _logger.LogInformation($"Registered MSBuild {instance.Version} instance at {instance.Path}");
            return true;
        }
    }

    private MSBuildLocation? FindMSBuild(string projectOrSolutionFilePath, bool includeUnloadableInstances)
    {
        if (!PlatformInformation.IsRunningOnMono)
        {
            VisualStudioInstance? instance;

#if NETFRAMEWORK
            // In this case, we're just going to pick the highest VS install on the machine, in case the projects are using some newer
            // MSBuild features. Since we don't have something like a global.json we can't really know what the minimum version is.

            // TODO: we should also check that the managed tools are actually installed
            instance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(vs => vs.Version).FirstOrDefault();
#else
            // Locate the right SDK for this particular project; MSBuildLocator ensures in this case the first one is the preferred one.
            // The includeUnloadableInstance parameter additionally locates SDKs from all installations regardless of whether they are
            // loadable by the BuildHost process.
            var options = new VisualStudioInstanceQueryOptions
            {
                DiscoveryTypes = DiscoveryType.DotNetSdk,
                WorkingDirectory = Path.GetDirectoryName(projectOrSolutionFilePath),
                AllowAllDotnetLocations = includeUnloadableInstances,
                AllowAllRuntimeVersions = includeUnloadableInstances,
            };

            instance = MSBuildLocator.QueryVisualStudioInstances(options).FirstOrDefault();
#endif

            if (instance != null)
            {
                return new(instance.MSBuildPath, instance.Version.ToString());
            }

            _logger.LogCritical("No compatible MSBuild instance could be found.");
        }
        else
        {

#if NETFRAMEWORK
            // We're running on Mono, but not all Mono installations have a usable MSBuild installation, so let's see if we have one that we can use.
            var monoMSBuildDirectory = MonoMSBuildDiscovery.GetMonoMSBuildDirectory();
            if (monoMSBuildDirectory != null)
            {
                var monoMSBuildVersion = MonoMSBuildDiscovery.GetMonoMSBuildVersion();
                if (monoMSBuildVersion != null)
                {
                    return new(monoMSBuildDirectory, monoMSBuildVersion);
                }
            }

            _logger.LogCritical("No Mono MSBuild installation could be found; see https://www.mono-project.com/ for installation instructions.");
#else
            _logger.LogCritical("Trying to run the .NET Core BuildHost on Mono is unsupported.");
#endif

        }

        return null;
    }

    [MemberNotNull(nameof(_buildManager))]
    [MethodImpl(MethodImplOptions.NoInlining)] // Do not inline this, since this creates MSBuild types which are being loaded by the caller
    private void CreateBuildManager()
    {
        lock (_gate)
        {
            if (_buildManager != null)
                return;

            if (_globalMSBuildProperties is null)
                throw new InvalidOperationException($"{nameof(ConfigureGlobalState)} should have been called first to set up global state.");

            BinaryLogger? logger = null;

            if (_binaryLogPath != null)
            {
                logger = new BinaryLogger { Parameters = _binaryLogPath };
                _logger.LogInformation($"Logging builds to {_binaryLogPath}");
            }

            _buildManager = new ProjectBuildManager(_globalMSBuildProperties, logger);
            _buildManager.StartBatchBuild(_globalMSBuildProperties);
        }
    }

    public MSBuildLocation? FindBestMSBuild(string projectOrSolutionFilePath)
    {
        return FindMSBuild(projectOrSolutionFilePath, includeUnloadableInstances: true);
    }

    public bool HasUsableMSBuild(string projectOrSolutionFilePath)
    {
        return TryEnsureMSBuildLoaded(projectOrSolutionFilePath);
    }

    private void EnsureMSBuildLoaded(string projectFilePath)
    {
        Contract.ThrowIfFalse(TryEnsureMSBuildLoaded(projectFilePath), $"We don't have an MSBuild to use; {nameof(HasUsableMSBuild)} should have been called first to check.");
    }

    public void ConfigureGlobalState(Dictionary<string, string> globalProperties, string? binlogPath)
    {
        lock (_gate)
        {
            if (_buildManager != null)
                throw new InvalidOperationException($"{nameof(_buildManager)} has already been initialized and cannot be changed");

            _globalMSBuildProperties = globalProperties;
            _binaryLogPath = binlogPath;
        }
    }

    /// <summary>
    /// Returns the target ID of the <see cref="ProjectFile"/> object created for this.
    /// </summary>
    public Task<int> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(projectFilePath);
        return LoadProjectFileCoreAsync(projectFilePath, languageName, cancellationToken);
    }

    /// <summary>
    /// Returns the target ID of the <see cref="ProjectFile"/> object created for this.
    /// </summary>
    public int LoadProject(string projectFilePath, string projectContent, string languageName)
    {
        EnsureMSBuildLoaded(projectFilePath);
        return LoadProjectCore(projectFilePath, projectContent, languageName);
    }

    // When using the Mono runtime, the MSBuild types used in this method must be available
    // to the JIT during compilation of the method, so they have to be loaded by the caller;
    // therefore this method must not be inlined.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<int> LoadProjectFileCoreAsync(string projectFilePath, string languageName, CancellationToken cancellationToken)
    {
        CreateBuildManager();

        _logger.LogInformation($"Loading {projectFilePath}");

        var (project, log) = await _buildManager.LoadProjectAsync(projectFilePath, cancellationToken).ConfigureAwait(false);
        return AddProjectFileTarget(project, languageName, log);
    }

    // When using the Mono runtime, the MSBuild types used in this method must be available
    // to the JIT during compilation of the method, so they have to be loaded by the caller;
    // therefore this method must not be inlined.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int LoadProjectCore(string projectFilePath, string projectContent, string languageName)
    {
        CreateBuildManager();

        _logger.LogInformation($"Loading an in-memory project with the path {projectFilePath}");

        // We expect MSBuild to consume this stream with a utf-8 encoding.
        // This is because we expect the stream we create to not include a BOM nor an an encoding declaration a la `<?xml encoding="..."?>`.
        // In this scenario, the XML standard requires XML processors to consume the document with a UTF-8 encoding.
        // https://www.w3.org/TR/xml/#d0e4623
        // Theoretically we could also enforce that 'projectContent' does not contain an encoding declaration with non-UTF-8 encoding.
        // But it seems like a very unlikely scenario to actually get into--this is not something people generally put on real project files.
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(projectContent));

        var (project, log) = _buildManager.LoadProject(projectFilePath, stream);
        return AddProjectFileTarget(project, languageName, log);
    }

    private int AddProjectFileTarget(Build.Evaluation.Project? project, string languageName, DiagnosticLog log)
    {
        Contract.ThrowIfNull(_buildManager);

        var projectFile = new ProjectFile(
            languageName,
            project,
            ProjectCommandLineProvider.Create(languageName),
            _buildManager,
            log);

        return _server.AddTarget(projectFile);
    }

    public Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(projectFilePath);
        CreateBuildManager();

        return _buildManager.TryGetOutputFilePathAsync(projectFilePath, cancellationToken);
    }

    public async Task ShutdownAsync()
    {
        _buildManager?.EndBatchBuild();

        _server.Shutdown();
    }
}
