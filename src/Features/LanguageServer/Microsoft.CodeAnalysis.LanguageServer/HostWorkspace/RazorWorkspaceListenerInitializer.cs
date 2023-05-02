// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.ExternalAccess.RoslynWorkspace;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(RazorWorkspaceListenerInitializer)), Shared]
internal sealed class RazorWorkspaceListenerInitializer
{
    // This should be moved to the Razor side once things are announced, so defaults are all in one
    // place, in case things ever need to change
    private const string _projectRazorJsonFileName = "project.razor.vscode.json";

    private readonly ILogger _logger;
    private readonly HashSet<ProjectId> _projectIdWithDynamicFiles = new();
    private readonly object _initializeGate = new();
    private readonly Lazy<LanguageServerWorkspaceFactory> _workspaceFactory;
    private readonly ILoggerFactory _loggerFactory;

    private RazorWorkspaceListener? _razorWorkspaceListener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorWorkspaceListenerInitializer(Lazy<LanguageServerWorkspaceFactory> workspaceFactory, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(RazorWorkspaceListenerInitializer));

        _workspaceFactory = workspaceFactory;
        _loggerFactory = loggerFactory;
    }

    internal void Initialize()
    {
        lock (_initializeGate)
        {
            // Only initialize once
            if (_razorWorkspaceListener is not null)
            {
                return;
            }

            _logger.LogTrace("Initializing the Razor workspace listener");
            _razorWorkspaceListener = new RazorWorkspaceListener(_loggerFactory);
            _razorWorkspaceListener.EnsureInitialized(_workspaceFactory.Value.Workspace, _projectRazorJsonFileName);
        }

        foreach (var projectId in _projectIdWithDynamicFiles)
        {
            _logger.LogTrace("{projectId} notifying a dynamic file for the first time", projectId);
            _razorWorkspaceListener.NotifyDynamicFile(projectId);
        }

        // May as well clear out the collection, it will never get used again anyway.
        _projectIdWithDynamicFiles.Clear();
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
