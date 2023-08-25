﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;

[ExportCSharpVisualBasicStatelessLspService(typeof(WorkspaceDebugConfigurationHandler)), Shared]
[Method(MethodName)]
internal class WorkspaceDebugConfigurationHandler : ILspServiceRequestHandler<WorkspaceDebugConfigurationParams, ProjectDebugConfiguration[]>
{
    private const string MethodName = "workspace/debugConfiguration";

    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceDebugConfigurationHandler(ProjectTargetFrameworkManager targetFrameworkManager)
    {
        _targetFrameworkManager = targetFrameworkManager;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public Task<ProjectDebugConfiguration[]> HandleRequestAsync(WorkspaceDebugConfigurationParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution, nameof(context.Solution));

        var projects = context.Solution.Projects
            .Where(p => p.FilePath != null && p.OutputFilePath != null)
            .Where(p => IsProjectInWorkspace(request.WorkspacePath, p))
            .Select(GetProjectDebugConfiguration).ToArray();
        return Task.FromResult(projects);
    }

    private static bool IsProjectInWorkspace(Uri workspacePath, Project project)
    {
        return PathUtilities.IsSameDirectoryOrChildOf(project.FilePath!, workspacePath.LocalPath);
    }

    private ProjectDebugConfiguration GetProjectDebugConfiguration(Project project)
    {
        var isExe = project.CompilationOptions?.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;
        var targetsDotnetCore = _targetFrameworkManager.IsDotnetCoreProject(project.Id);
        return new ProjectDebugConfiguration(project.FilePath!, project.OutputFilePath!, GetProjectName(project), targetsDotnetCore, isExe, project.Solution.FilePath);
    }

    private static string GetProjectName(Project project)
    {
        var (_, flavor) = project.State.NameAndFlavor;
        if (string.IsNullOrEmpty(flavor))
        {
            return project.Name;
        }
        else
        {
            var projectPath = project.FilePath!;
            var projectFileName = Path.GetFileName(projectPath);
            return $"{projectFileName} ({flavor}) - {projectPath}";
        }
    }
}
