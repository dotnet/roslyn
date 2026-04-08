namespace LspVGrepTool.Infrastructure;

internal enum RoslynLoadTargetKind
{
    Solution,
    Project
}

internal sealed record RoslynLoadTarget(string Path, RoslynLoadTargetKind Kind);

internal static class SolutionDiscovery
{
    public static RoslynLoadTarget Find(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"The configured directory '{directoryPath}' does not exist.");
        }

        // Prefer .slnx, then .sln, then .csproj — shallowest first, alphabetical tiebreak.
        var slnxFiles = EnumerateCandidates(directoryPath, "*.slnx");
        if (slnxFiles.Count > 0)
        {
            return new RoslynLoadTarget(slnxFiles[0], RoslynLoadTargetKind.Solution);
        }

        var solutions = EnumerateCandidates(directoryPath, "*.sln");
        if (solutions.Count > 0)
        {
            return new RoslynLoadTarget(solutions[0], RoslynLoadTargetKind.Solution);
        }

        var projects = EnumerateCandidates(directoryPath, "*.csproj");
        if (projects.Count > 0)
        {
            return new RoslynLoadTarget(projects[0], RoslynLoadTargetKind.Project);
        }

        throw new FileNotFoundException(
            $"No .sln or .csproj files were found under '{directoryPath}'.");
    }

    private static List<string> EnumerateCandidates(string directoryPath, string searchPattern)
    {
        return Directory
            .EnumerateFiles(directoryPath, searchPattern, SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path))
            .OrderBy(path => GetRelativeDepth(directoryPath, path))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsBuildArtifact(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static int GetRelativeDepth(string rootPath, string candidatePath)
    {
        var relativePath = Path.GetRelativePath(rootPath, candidatePath);
        return relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
    }
}
