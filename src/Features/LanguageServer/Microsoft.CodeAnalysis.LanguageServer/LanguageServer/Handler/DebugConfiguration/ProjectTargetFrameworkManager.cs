// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;

/// <summary>
/// Keeps track of which project uses what TFM.
/// </summary>
[Export, Shared]
internal class ProjectTargetFrameworkManager
{
    private readonly ConcurrentDictionary<ProjectId, string?> _projectToTargetFrameworkIdentifer = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ProjectTargetFrameworkManager()
    {
    }

    public void UpdateIdentifierForProject(ProjectId projectId, string? identifier)
    {
        _ = _projectToTargetFrameworkIdentifer.AddOrUpdate(projectId, identifier, (project, oldIdentifier) => identifier);
    }

    public bool IsDotnetCoreProject(ProjectId projectId)
    {
        if (_projectToTargetFrameworkIdentifer.TryGetValue(projectId, out var identifier) && identifier != null)
        {
            return IsDotnetCoreIdentifier(identifier);
        }

        return false;
    }

    private static bool IsDotnetCoreIdentifier(string identifier)
    {
        // This is the condition suggested by the SDK/MSBuild for determining if a project targets .net core.
        return identifier.StartsWith(".NETCoreApp") || identifier.StartsWith(".NETStandard");
    }
}
