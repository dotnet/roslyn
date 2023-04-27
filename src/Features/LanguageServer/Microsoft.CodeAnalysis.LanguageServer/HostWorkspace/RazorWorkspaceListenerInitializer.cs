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
    private static string _projectRazorJsonFileName = "project.razor.vscode.json";

    internal static void SetProjectRazorJsonFileName(string projectRazorJsonFileName)
    {
        _projectRazorJsonFileName = projectRazorJsonFileName;
    }

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

        lock (_initializeGate)
        {
            if (_razorWorkspaceListener.IsValueCreated)
            {
                return;
            }

            _logger.LogTrace("Initializing the Razor workspace listener");
            _ = _razorWorkspaceListener.Value;
        }

        foreach (var projectId in _projectIdWithDynamicFiles)
        {
            _logger.LogTrace("{projectId} notifying a dynamic file for the first time", projectId);
            _razorWorkspaceListener.Value.NotifyDynamicFile(projectId);
        }
    }

    internal void NotifyDynamicFile(ProjectId projectId)
    {
        if (_razorWorkspaceListener.IsValueCreated)
        {
            // We've been initialized, so just pass the information along
            _logger.LogTrace("{projectId} forwarding on a dynamic file notification because we're initialized", projectId);
            _razorWorkspaceListener.Value.NotifyDynamicFile(projectId);
            return;
        }

        _logger.LogTrace("{projectId} queuing up a dynamic file notify for later", projectId);
        // We haven't been initialized by the extension yet, so just queue up the project for later
        ImmutableInterlocked.Update(ref _projectIdWithDynamicFiles, (col, arg) => col.Add(arg), projectId);
    }
}
