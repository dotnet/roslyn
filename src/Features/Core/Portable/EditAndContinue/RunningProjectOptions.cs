// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
internal readonly struct RunningProjectOptions
{
    /// <summary>
    /// Required restart of the project when an edit that has no effect until the app is restarted is made to any dependent project.
    /// </summary>
    [DataMember]
    public required bool RestartWhenChangesHaveNoEffect { get; init; }
}

internal static class RunningProjectOptionsFactory
{
    public static ImmutableDictionary<ProjectId, RunningProjectOptions> ToRunningProjectOptions<TInfo>(
        this ImmutableArray<TInfo> runningProjects,
        Solution solution,
        Func<TInfo, (string projectPath, string targetFramework, bool restartAutomatically)> translator)
    {
        // Invariants guaranteed by the debugger:
        // - Running projects does nto contain duplicate ids.
        // - TFM is always specified for SDK projects event if the project doesn't multi-target, it is empty for legacy projects.

        var runningProjectsByPathAndTfm = runningProjects
            .Select(info =>
            {
                var (filePath, targetFramework, restartAutomatically) = translator(info);
                return KeyValuePair.Create((filePath, targetFramework is { Length: > 0 } tfm ? tfm : null), restartAutomatically);
            })
            .ToImmutableDictionary(PathAndTfmComparer.Instance);

        var result = ImmutableDictionary.CreateBuilder<ProjectId, RunningProjectOptions>();

        foreach (var project in solution.Projects)
        {
            if (project.FilePath == null)
            {
                continue;
            }

            // Roslyn project name does not include TFM if the project is not multi-targeted (flavor is null).
            // The key comparer ignores TFM if null and therefore returns a random entry that has the same file path.
            // Since projects without TFM can only have at most one entry in the dictionary the random entry is that single value.
            if (runningProjectsByPathAndTfm.TryGetValue((project.FilePath, project.State.NameAndFlavor.flavor), out var restartAutomatically))
            {
                result.Add(project.Id, new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = restartAutomatically });
                continue;
            }
        }

        return result.ToImmutableDictionary();
    }

    private sealed class PathAndTfmComparer : IEqualityComparer<(string path, string? tfm)>
    {
        public static readonly PathAndTfmComparer Instance = new();

        public int GetHashCode((string path, string? tfm) obj)
            => obj.path.GetHashCode(); // only hash path, all tfms need to fall to the same bucket

        public bool Equals((string path, string? tfm) x, (string path, string? tfm) y)
            => x.path == y.path && (x.tfm == null || y.tfm == null || x.tfm == y.tfm);
    }
}
