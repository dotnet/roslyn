using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace LspVGrepTool.Infrastructure;

internal sealed record WorkspaceLoadResult(
    MSBuildWorkspace Workspace,
    Solution Solution,
    string TargetPath,
    RoslynLoadTargetKind TargetKind);

internal sealed class RoslynWorkspaceProvider : IDisposable
{
    private MSBuildWorkspace? _workspace;

    public async Task<WorkspaceLoadResult> LoadAsync(string directoryPath, CancellationToken cancellationToken)
    {
        MsBuildRegistration.EnsureRegistered();

        var target = SolutionDiscovery.Find(directoryPath);
        var workspace = MSBuildWorkspace.Create();
        _workspace = workspace;

        Solution solution;
        switch (target.Kind)
        {
            case RoslynLoadTargetKind.Solution:
                solution = await workspace.OpenSolutionAsync(target.Paths[0], cancellationToken: cancellationToken);
                break;

            case RoslynLoadTargetKind.Project:
                solution = (await workspace.OpenProjectAsync(target.Paths[0], cancellationToken: cancellationToken)).Solution;
                break;

            case RoslynLoadTargetKind.MultipleProjects:
                foreach (var projectPath in target.Paths)
                {
                    try
                    {
                        await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
                    }
                    catch (Exception)
                    {
                        // Some projects may conflict (duplicate assembly names, etc.) — skip them.
                    }
                }
                solution = workspace.CurrentSolution;
                break;

            default:
                throw new InvalidOperationException($"Unsupported Roslyn load target kind '{target.Kind}'.");
        }

        // Warm up compilations so individual algorithm timings don't include lazy compilation cost.
        await Task.WhenAll(solution.Projects.Select(p => p.GetCompilationAsync(cancellationToken)));

        return new WorkspaceLoadResult(workspace, solution, target.DisplayPath, target.Kind);
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
