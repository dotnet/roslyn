using System.Text.Json;

namespace LspVGrepTool.Infrastructure;

internal enum RoslynLoadTargetKind
{
    Solution,
    Project,
    MultipleProjects
}

internal sealed record RoslynLoadTarget(RoslynLoadTargetKind Kind, IReadOnlyList<string> Paths)
{
    /// <summary>
    /// Returns the single path for <see cref="RoslynLoadTargetKind.Solution"/> and
    /// <see cref="RoslynLoadTargetKind.Project"/>, or a summary for <see cref="RoslynLoadTargetKind.MultipleProjects"/>.
    /// </summary>
    public string DisplayPath => Kind == RoslynLoadTargetKind.MultipleProjects
        ? $"{Paths.Count} projects"
        : Paths[0];
}

/// <summary>
/// Discovers what to load using the same priority order as the Roslyn language server:
/// 1. <c>.vscode/settings.json</c> → <c>dotnet.defaultSolution</c>
/// 2. Exactly one <c>.sln</c> / <c>.slnx</c> at the root directory
/// 3. All <c>.csproj</c> files discovered recursively
/// </summary>
internal static class SolutionDiscovery
{
    public static RoslynLoadTarget Find(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"The configured directory '{directoryPath}' does not exist.");
        }

        // 1. Check .vscode/settings.json for dotnet.defaultSolution.
        var solutionFromSettings = TryGetDefaultSolutionFromVSCodeSettings(directoryPath);
        if (solutionFromSettings is not null)
        {
            return new RoslynLoadTarget(RoslynLoadTargetKind.Solution, [solutionFromSettings]);
        }

        // 2. If exactly one .sln or .slnx exists at the root, load it.
        var rootSolutions = Directory.EnumerateFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(directoryPath, "*.slnx", SearchOption.TopDirectoryOnly))
            .ToArray();

        if (rootSolutions.Length == 1)
        {
            return new RoslynLoadTarget(RoslynLoadTargetKind.Solution, [rootSolutions[0]]);
        }

        // 3. Fall back to discovering all .csproj files recursively, excluding test projects.
        var projects = Directory
            .EnumerateFiles(directoryPath, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path) && !IsTestProject(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (projects.Count == 0)
        {
            throw new FileNotFoundException(
                $"No solution or project files were found under '{directoryPath}'.");
        }

        if (projects.Count == 1)
        {
            return new RoslynLoadTarget(RoslynLoadTargetKind.Project, projects);
        }

        return new RoslynLoadTarget(RoslynLoadTargetKind.MultipleProjects, projects);
    }

    private static string? TryGetDefaultSolutionFromVSCodeSettings(string directoryPath)
    {
        var settingsPath = Path.Combine(directoryPath, ".vscode", "settings.json");
        if (!File.Exists(settingsPath))
            return null;

        try
        {
            var json = File.ReadAllText(settingsPath);
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (!doc.RootElement.TryGetProperty("dotnet.defaultSolution", out var value))
                return null;

            var defaultSolution = value.GetString();
            if (string.IsNullOrWhiteSpace(defaultSolution) || string.Equals(defaultSolution, "disable", StringComparison.Ordinal))
                return null;

            var solutionPath = Path.IsPathRooted(defaultSolution)
                ? defaultSolution
                : Path.GetFullPath(Path.Combine(directoryPath, defaultSolution));

            return File.Exists(solutionPath) ? solutionPath : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsBuildArtifact(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("artifacts", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTestProject(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);

        // File name contains ".Test." / ".Tests." or ends with ".Test" / ".Tests"
        if (fileName.Contains(".Test.", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains(".Tests.", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Project lives under a "test" or "tests" directory segment
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals("test", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("tests", StringComparison.OrdinalIgnoreCase));
    }
}
