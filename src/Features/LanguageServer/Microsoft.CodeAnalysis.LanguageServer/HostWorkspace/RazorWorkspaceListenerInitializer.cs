// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.AspNetCore.Razor.ExternalAccess.RoslynWorkspace;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(RazorWorkspaceListenerInitializer)), Shared]
internal sealed class RazorWorkspaceListenerInitializer
{
    // This should be moved to the Razor side once things are announced, so defaults are all in one
    // place, in case things ever need to change
    private const string _projectRazorJsonFileName = "project.razor.vscode.bin";

    private readonly ILogger _logger;
    private readonly Workspace _workspace;
    private readonly ILoggerFactory _loggerFactory;

    // Locks all access to _razorWorkspaceListener and _projectIdWithDynamicFiles
    private readonly object _initializeGate = new();
    private HashSet<ProjectId> _projectIdWithDynamicFiles = new();

    private RazorWorkspaceListener? _razorWorkspaceListener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorWorkspaceListenerInitializer(LanguageServerWorkspaceFactory workspaceFactory, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(RazorWorkspaceListenerInitializer));

        _workspace = workspaceFactory.Workspace;
        _loggerFactory = loggerFactory;
    }

    internal void Initialize()
    {
        HashSet<ProjectId> projectsToInitialize;
        lock (_initializeGate)
        {
            // Only initialize once
            if (_razorWorkspaceListener is not null)
            {
                return;
            }

            _logger.LogTrace("Initializing the Razor workspace listener");
            _razorWorkspaceListener = new RazorWorkspaceListener(_loggerFactory);
            _razorWorkspaceListener.EnsureInitialized(_workspace, _projectRazorJsonFileName);

            projectsToInitialize = _projectIdWithDynamicFiles;
            // May as well clear out the collection, it will never get used again anyway.
            _projectIdWithDynamicFiles = new();
        }

        foreach (var projectId in projectsToInitialize)
        {
            _logger.LogTrace("{projectId} notifying a dynamic file for the first time", projectId);
            _razorWorkspaceListener.NotifyDynamicFile(projectId);
        }
    }

    internal void NotifyDynamicFile(ProjectId projectId)
    {
        lock (_initializeGate)
        {
            if (_razorWorkspaceListener is null)
            {
                // We haven't been initialized by the extension yet, so just store the project id, to tell Razor later
                _logger.LogTrace("{projectId} queuing up a dynamic file notify for later", projectId);
                _projectIdWithDynamicFiles.Add(projectId);

                return;
            }
        }

        // We've been initialized, so just pass the information along
        _logger.LogTrace("{projectId} forwarding on a dynamic file notification because we're initialized", projectId);
        _razorWorkspaceListener.NotifyDynamicFile(projectId);
    }
}
