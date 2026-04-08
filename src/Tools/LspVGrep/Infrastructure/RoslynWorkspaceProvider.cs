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

        Solution solution = target.Kind switch
        {
            RoslynLoadTargetKind.Solution => await workspace.OpenSolutionAsync(target.Path, cancellationToken: cancellationToken),
            RoslynLoadTargetKind.Project => (await workspace.OpenProjectAsync(target.Path, cancellationToken: cancellationToken)).Solution,
            _ => throw new InvalidOperationException($"Unsupported Roslyn load target kind '{target.Kind}'.")
        };

        // Warm up compilations so individual algorithm timings don't include lazy compilation cost.
        await Task.WhenAll(solution.Projects.Select(p => p.GetCompilationAsync(cancellationToken)));

        return new WorkspaceLoadResult(workspace, solution, target.Path, target.Kind);
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
