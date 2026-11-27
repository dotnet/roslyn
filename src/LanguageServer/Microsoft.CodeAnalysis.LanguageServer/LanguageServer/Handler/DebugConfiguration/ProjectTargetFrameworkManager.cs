// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportCSharpVisualBasicLspServiceFactory(typeof(ProjectTargetFrameworkManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ProjectTargetFrameworkManagerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new ProjectTargetFrameworkManager();
}

/// <summary>
/// Keeps track of which project uses what TFM.
/// </summary>
internal sealed class ProjectTargetFrameworkManager() : ILspService
{
    private readonly ConcurrentDictionary<ProjectId, string?> _projectToTargetFrameworkIdentifer = new();
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
