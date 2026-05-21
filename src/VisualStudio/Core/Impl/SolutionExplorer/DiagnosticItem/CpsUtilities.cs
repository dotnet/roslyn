// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal static class CpsUtilities
{
    /// <summary>
    /// Given the canonical name of a node representing an analyzer assembly in the
    /// CPS-based project system extracts out the full path to the assembly.
    /// </summary>
    /// <param name="projectDirectoryPath">The full path to the project directory</param>
    /// <param name="analyzerNodeCanonicalName">The canonical name of the analyzer node in the hierarchy</param>
    /// <returns>The full path to the analyzer assembly on disk, or null if <paramref name="analyzerNodeCanonicalName"/>
    /// cannot be parsed.</returns>
    /// <remarks>
    /// Two forms of canonical name are supported:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Legacy form (pre-VS16.7): <c>[{path to project directory}\]{target framework}\analyzerdependency\{path to assembly}</c>,
    /// e.g. <c>C:\projects\solutions\MyProj\netstandard2.0\analyzerdependency\C:\users\me\.packages\somePackage\lib\someAnalyzer.dll</c>.
    /// The leading project-directory and <c>{tfm}\analyzerdependency\</c> portions are CPS tree decoration describing how
    /// the dependency was discovered; the analyzer's actual file path always comes after <c>analyzerdependency\</c> and is
    /// always rooted. The project-directory prefix is optional.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Newer form (VS16.7+): the canonical name is the analyzer assembly's full file path. No processing is required and
    /// the value must be returned unchanged so it matches <c>AnalyzerReference.FullPath</c>, including when the analyzer
    /// physically lives under the project directory (e.g. <c>&lt;Analyzer Include="Analyzers\MyAnalyzer.dll" /&gt;</c>).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public static string? ExtractAnalyzerFilePath(string projectDirectoryPath, string analyzerNodeCanonicalName)
    {
        const string AnalyzerDependencySegment = @"analyzerdependency\";

        if (!projectDirectoryPath.EndsWith("\\"))
        {
            projectDirectoryPath += '\\';
        }

        // The legacy-form canonical name may start with the path to the project's directory as decoration.
        // We only strip that prefix for the purpose of detecting the legacy form; for the newer form we must
        // return the original canonical name unchanged so that analyzers physically located under the project
        // directory are not converted to project-relative paths (which would fail to match AnalyzerReference.FullPath).
        var remaining = analyzerNodeCanonicalName;
        if (remaining.StartsWith(projectDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            remaining = remaining[projectDirectoryPath.Length..];
        }

        // Find the slash after the target framework
        var backslashIndex = remaining.IndexOf('\\');
        if (backslashIndex < 0)
        {
            return null;
        }

        // If the path does not contain "analyzerdependency\" immediately after the first slash, it
        // is the newer form of the analyzer tree item's file path (VS16.7+) which requires no processing.
        // The length guard avoids ArgumentOutOfRangeException from the bounded IndexOf overload when
        // the remaining string is too short to contain the segment at this offset (e.g. a malformed
        // canonical name ending with a trailing backslash).
        var segmentStart = backslashIndex + 1;
        if (remaining.Length < segmentStart + AnalyzerDependencySegment.Length ||
            remaining.IndexOf(AnalyzerDependencySegment, segmentStart, AnalyzerDependencySegment.Length, StringComparison.OrdinalIgnoreCase) != segmentStart)
        {
            return analyzerNodeCanonicalName;
        }

        // Find the slash after "analyzerdependency"
        backslashIndex = remaining.IndexOf('\\', backslashIndex + 1);
        if (backslashIndex < 0)
        {
            return null;
        }

        // The rest of the string is the analyzer path. In the legacy form this is always rooted; if it isn't,
        // we matched "analyzerdependency\" coincidentally inside a newer-form path that happens to live under
        // the project directory (e.g. an analyzer at "{projectDir}\netstandard2.0\analyzerdependency\Foo.dll").
        // Fall back to the newer-form interpretation in that case.
        var candidate = remaining[(backslashIndex + 1)..];
        if (!PathUtilities.IsAbsolute(candidate))
        {
            return analyzerNodeCanonicalName;
        }

        return candidate;
    }
}
