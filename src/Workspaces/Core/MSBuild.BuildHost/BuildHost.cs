// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

    private void EnsureMSBuildLoaded(string projectFilePath)
    {
        lock (_gate)
        {
            // If we've already created our MSBuild types, then there's nothing further to do.
            if (MSBuildLocator.IsRegistered)
            {
                return;
            }

            VisualStudioInstance instance;

#if NETFRAMEWORK

            // In this case, we're just going to pick the highest VS install on the machine, in case the projects are using some newer
            // MSBuild features. Since we don't have something like a global.json we can't really know what the minimum version is.

            // TODO: we should also check that the managed tools are actually installed
            instance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(vs => vs.Version).First();

#else

            // Locate the right SDK for this particular project; MSBuildLocator ensures in this case the first one is the preferred one.
            // TODO: we should pick the appropriate instance back in the main process and just use the one chosen here.
            var options = new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = Path.GetDirectoryName(projectFilePath) };
            instance = MSBuildLocator.QueryVisualStudioInstances(options).First();

#endif

            MSBuildLocator.RegisterInstance(instance);
            _logger.LogInformation($"Registered MSBuild instance at {instance.MSBuildPath}");
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
