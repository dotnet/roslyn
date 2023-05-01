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
    private readonly Lazy<RazorWorkspaceListener> _razorWorkspaceListener;
    private ImmutableHashSet<ProjectId> _projectIdWithDynamicFiles = ImmutableHashSet<ProjectId>.Empty;
    private readonly object _initializeGate = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorWorkspaceListenerInitializer(Lazy<LanguageServerWorkspaceFactory> workspaceFactory, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(RazorWorkspaceListenerInitializer));

        _razorWorkspaceListener = new Lazy<RazorWorkspaceListener>(() =>
        {
            var razorWorkspaceListener = new RazorWorkspaceListener(loggerFactory);
            var workspace = workspaceFactory.Value.Workspace;
            razorWorkspaceListener.EnsureInitialized(workspace, _projectRazorJsonFileName);

            return razorWorkspaceListener;
        });
    }

    internal void Initialize()
    {
        // Only initialize once
        if (_razorWorkspaceListener.IsValueCreated)
        {
            return;
        }

        ImmutableHashSet<ProjectId> projectsToProcess;
        lock (_initializeGate)
        {
            if (_razorWorkspaceListener.IsValueCreated)
            {
                return;
            }

            _logger.LogTrace("Initializing the Razor workspace listener");
            _ = _razorWorkspaceListener.Value;
            projectsToProcess = Interlocked.Exchange(ref _projectIdWithDynamicFiles, ImmutableHashSet<ProjectId>.Empty);
        }

        foreach (var projectId in projectsToProcess)
        {
            _logger.LogTrace("{projectId} notifying a dynamic file for the first time", projectId);
            _razorWorkspaceListener.Value.NotifyDynamicFile(projectId);
        }
    }

    internal void NotifyDynamicFile(ProjectId projectId)
    {
        if (!_razorWorkspaceListener.IsValueCreated)
        {
            // We haven't been initialized by the extension yet, so just store the project id, to tell Razor later
            _logger.LogTrace("{projectId} queuing up a dynamic file notify for later", projectId);
            ImmutableInterlocked.Update(ref _projectIdWithDynamicFiles, (col, arg) => col.Add(arg), projectId);

            return;
        }

        // We've been initialized, so just pass the information along
        _logger.LogTrace("{projectId} forwarding on a dynamic file notification because we're initialized", projectId);
        _razorWorkspaceListener.Value.NotifyDynamicFile(projectId);

        // It's possible that we were initialized after the IsValueCreated check above, which could leave stale project Ids in
        // our hashset, so we'll see if we need to clear it out just in case
        if (!_projectIdWithDynamicFiles.IsEmpty)
        {
            var projects = Interlocked.Exchange(ref _projectIdWithDynamicFiles, ImmutableHashSet<ProjectId>.Empty);
            foreach (var pId in projects)
            {
                _razorWorkspaceListener.Value.NotifyDynamicFile(pId);
            }
        }
    }
}
