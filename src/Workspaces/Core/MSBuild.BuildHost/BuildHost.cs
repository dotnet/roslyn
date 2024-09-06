// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHost : IBuildHost
{
    private readonly BuildHostLogger _logger;
    private readonly ImmutableDictionary<string, string> _globalMSBuildProperties;
    private readonly string? _binaryLogPath;
    private readonly RpcServer _server;
    private readonly object _gate = new object();
    private ProjectBuildManager? _buildManager;

    public BuildHost(BuildHostLogger logger, ImmutableDictionary<string, string> globalMSBuildProperties, string? binaryLogPath, RpcServer server)
    {
        _logger = logger;
        _globalMSBuildProperties = globalMSBuildProperties;
        _binaryLogPath = binaryLogPath;
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
                // TODO: we should pick the appropriate instance back in the main process and just use the one chosen here.
                var options = new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = Path.GetDirectoryName(projectOrSolutionFilePath) };
                instance = MSBuildLocator.QueryVisualStudioInstances(options).FirstOrDefault();

#endif

                if (instance != null)
                {
                    MSBuildLocator.RegisterInstance(instance);
                    _logger.LogInformation($"Registered MSBuild instance at {instance.MSBuildPath}");
                }
                else
                {
                    _logger.LogCritical("No compatible MSBuild instance could be found.");
                }
            }
            else
            {
#if NETFRAMEWORK

                // We're running on Mono, but not all Mono installations have a usable MSBuild installation, so let's see if we have one that we can use.
                var monoMSBuildDirectory = MonoMSBuildDiscovery.GetMonoMSBuildDirectory();

                if (monoMSBuildDirectory != null)
                {
                    MSBuildLocator.RegisterMSBuildPath(monoMSBuildDirectory);
                    _logger.LogInformation($"Registered MSBuild instance at {monoMSBuildDirectory}");
                }
                else
                {
                    _logger.LogCritical("No Mono MSBuild installation could be found; see https://www.mono-project.com/ for installation instructions.");
                }

#else
                _logger.LogCritical("Trying to run the .NET Core BuildHost on Mono is unsupported.");
#endif
            }

            return MSBuildLocator.IsRegistered;
        }
    }

    [MemberNotNull(nameof(_buildManager))]
    [MethodImpl(MethodImplOptions.NoInlining)] // Do not inline this, since this creates MSBuild types which are being loaded by the caller
    private void CreateBuildManager()
    {
        lock (_gate)
        {
            if (_buildManager != null)
                return;

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

    public bool HasUsableMSBuild(string projectOrSolutionFilePath)
    {
        return TryEnsureMSBuildLoaded(projectOrSolutionFilePath);
    }

    private void EnsureMSBuildLoaded(string projectFilePath)
    {
        Contract.ThrowIfFalse(TryEnsureMSBuildLoaded(projectFilePath), $"We don't have an MSBuild to use; {nameof(HasUsableMSBuild)} should have been called first to check.");
    }

    public ImmutableArray<(string ProjectPath, string ProjectGuid)> GetProjectsInSolution(string solutionFilePath)
    {
        EnsureMSBuildLoaded(solutionFilePath);
        return GetProjectsInSolutionCore(solutionFilePath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // Do not inline this, since this uses MSBuild types which are being loaded by the caller
    private static ImmutableArray<(string ProjectPath, string ProjectGuid)> GetProjectsInSolutionCore(string solutionFilePath)
    {
        // WARNING: do not use a lambda in this function, as it internally will be put in a class that contains other lambdas used in
        // TryEnsureMSBuildLoaded; on Mono this causes type load errors.

        var builder = ImmutableArray.CreateBuilder<(string ProjectPath, string ProjectGuid)>();

        foreach (var project in SolutionFile.Parse(solutionFilePath).ProjectsInOrder)
        {
            if (project.ProjectType != SolutionProjectType.SolutionFolder)
            {
                builder.Add((project.AbsolutePath, project.ProjectGuid));
            }
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    /// Returns the target ID of the <see cref="ProjectFile"/> object created for this.
    /// </summary>
    public async Task<int> LoadProjectFileAsync(string projectFilePath, string languageName, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(projectFilePath);
        CreateBuildManager();

        ProjectFileLoader projectLoader = languageName switch
        {
            LanguageNames.CSharp => new CSharpProjectFileLoader(),
            LanguageNames.VisualBasic => new VisualBasicProjectFileLoader(),
            _ => throw ExceptionUtilities.UnexpectedValue(languageName)
        };

        _logger.LogInformation($"Loading {projectFilePath}");
        var projectFile = await projectLoader.LoadProjectFileAsync(projectFilePath, _buildManager, cancellationToken).ConfigureAwait(false);
        return _server.AddTarget(projectFile);
    }

    public Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(projectFilePath);
        CreateBuildManager();

        return _buildManager.TryGetOutputFilePathAsync(projectFilePath, cancellationToken);
    }

    public Task ShutdownAsync()
    {
        _buildManager?.EndBatchBuild();

        _server.Shutdown();

        return Task.CompletedTask;
    }
}
