// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;

[ExportCSharpVisualBasicStatelessLspService(typeof(WorkspaceDebugConfigurationHandler)), Shared]
[Method(MethodName)]
internal sealed class WorkspaceDebugConfigurationHandler : ILspServiceRequestHandler<WorkspaceDebugConfigurationParams, ProjectDebugConfiguration[]>
{
    private const string MethodName = "workspace/debugConfiguration";

    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;
    private readonly LanguageServerProjectSystem _projectSystem;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceDebugConfigurationHandler(ProjectTargetFrameworkManager targetFrameworkManager, LanguageServerProjectSystem projectSystem)
    {
        _targetFrameworkManager = targetFrameworkManager;
        _projectSystem = projectSystem;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<ProjectDebugConfiguration[]> HandleRequestAsync(WorkspaceDebugConfigurationParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution, nameof(context.Solution));

        var projects = context.Solution.Projects
            .Where(p => p is { FilePath: not null, OutputFilePath: not null })
            .Where(p => IsProjectInWorkspace(request.WorkspacePath, p))
            .Select(GetProjectDebugConfiguration).ToArray();
        return projects;
    }

    private static bool IsProjectInWorkspace(DocumentUri workspacePath, Project project)
    {
        return PathUtilities.IsSameDirectoryOrChildOf(project.FilePath!, workspacePath.GetRequiredParsedUri().LocalPath);
    }

    private ProjectDebugConfiguration GetProjectDebugConfiguration(Project project)
    {
        var isExe = project.CompilationOptions?.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;
        var targetsDotnetCore = _targetFrameworkManager.IsDotnetCoreProject(project.Id);
        return new ProjectDebugConfiguration(project.FilePath!, project.OutputFilePath!, GetProjectName(project), targetsDotnetCore, isExe, _projectSystem.SolutionPath);
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
            var projectPath = project.FilePath;
            var projectFileName = Path.GetFileName(projectPath);
            return $"{projectFileName} ({flavor}) - {projectPath}";
        }
    }
}
