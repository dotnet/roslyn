﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal sealed class BuildHost : IBuildHost
{
    private readonly ILogger _logger;
    private readonly string? _binaryLogPath;

    private readonly object _gate = new object();
    private ProjectBuildManager? _buildManager;

    public BuildHost(ILoggerFactory loggerFactory, string? binaryLogPath)
    {
        _logger = loggerFactory.CreateLogger<BuildHost>();
        _binaryLogPath = binaryLogPath;
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

            _buildManager = new ProjectBuildManager(ImmutableDictionary<string, string>.Empty, logger);
            _buildManager.StartBatchBuild();
        }
    }

    public Task<bool> HasUsableMSBuildAsync(string projectOrSolutionFilePath, CancellationToken cancellationToken)
    {
        return Task.FromResult(TryEnsureMSBuildLoaded(projectOrSolutionFilePath));
    }

    private void EnsureMSBuildLoaded(string projectFilePath)
    {
        Contract.ThrowIfFalse(TryEnsureMSBuildLoaded(projectFilePath), $"We don't have an MSBuild to use; {nameof(HasUsableMSBuildAsync)} should have been called first to check.");
    }

    public Task<ImmutableArray<(string ProjectPath, string ProjectGuid)>> GetProjectsInSolutionAsync(string solutionFilePath, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(solutionFilePath);
        return Task.FromResult(GetProjectsInSolution(solutionFilePath));
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // Do not inline this, since this uses MSBuild types which are being loaded by the caller
    private static ImmutableArray<(string ProjectPath, string ProjectGuid)> GetProjectsInSolution(string solutionFilePath)
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

        return builder.ToImmutable();
    }

    public Task<bool> IsProjectFileSupportedAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(projectFilePath);
        CreateBuildManager();

        return Task.FromResult(TryGetLoaderForPath(projectFilePath) is not null);
    }

    public async Task<IRemoteProjectFile> LoadProjectFileAsync(string projectFilePath, CancellationToken cancellationToken)
    {
        EnsureMSBuildLoaded(projectFilePath);
        CreateBuildManager();

        var projectLoader = TryGetLoaderForPath(projectFilePath);
        Contract.ThrowIfNull(projectLoader, $"We don't support this project path; we should have called {nameof(IsProjectFileSupportedAsync)} first.");
        _logger.LogInformation($"Loading {projectFilePath}");
        return new RemoteProjectFile(await projectLoader.LoadProjectFileAsync(projectFilePath, _buildManager, cancellationToken).ConfigureAwait(false));
    }

    private static IProjectFileLoader? TryGetLoaderForPath(string projectFilePath)
    {
        var extension = Path.GetExtension(projectFilePath);

        return extension switch
        {
            ".csproj" => new CSharp.CSharpProjectFileLoader(),
            ".vbproj" => new VisualBasic.VisualBasicProjectFileLoader(),
            _ => null
        };
    }

    public Task ShutdownAsync()
    {
        _buildManager?.EndBatchBuild();

        return Task.CompletedTask;
    }
}
