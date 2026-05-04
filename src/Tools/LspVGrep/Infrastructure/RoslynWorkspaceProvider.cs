using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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

        // Strip unresolved analyzer references so solution-wide operations
        // (FindImplementationsAsync, FindDerivedClassesAsync) don't crash during checksumming.
        solution = RemoveUnresolvedAnalyzerReferences(solution);

        // Warm up compilations so individual algorithm timings don't include lazy compilation cost.
        await Task.WhenAll(solution.Projects.Select(p => p.GetCompilationAsync(cancellationToken)));

        return new WorkspaceLoadResult(workspace, solution, target.DisplayPath, target.Kind);
    }

    private static Solution RemoveUnresolvedAnalyzerReferences(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            var resolved = project.AnalyzerReferences
                .Where(r => r is not UnresolvedAnalyzerReference)
                .ToList();

            if (resolved.Count != project.AnalyzerReferences.Count)
            {
                solution = solution.WithProjectAnalyzerReferences(project.Id, resolved);
            }
        }

        return solution;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
