// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal readonly record struct OnDemandProjectLoadResult
{
    public ImmutableDictionary<string, bool> ProjectCompleteness { get; }
    public ImmutableHashSet<string> LoadedProjects { get; }

    public static OnDemandProjectLoadResult Empty { get; } = new(
        ImmutableDictionary<string, bool>.Empty.WithComparers(PathUtilities.Comparer),
        ImmutableHashSet<string>.Empty.WithComparer(PathUtilities.Comparer));

    public OnDemandProjectLoadResult(ImmutableDictionary<string, bool> projectCompleteness)
        : this(
            projectCompleteness,
            projectCompleteness
                .Where(static pair => pair.Value)
                .Select(static pair => pair.Key)
                .ToImmutableHashSet(PathUtilities.Comparer))
    {
    }

    public OnDemandProjectLoadResult(
        ImmutableDictionary<string, bool> projectCompleteness,
        ImmutableHashSet<string> loadedProjects)
    {
        ProjectCompleteness = projectCompleteness;
        LoadedProjects = loadedProjects;
    }

    public bool IsProjectLoaded(string? projectFilePath)
        => projectFilePath is not null && LoadedProjects.Contains(projectFilePath);

    public bool HasCompleteDependencies(string? projectFilePath)
        => projectFilePath is not null &&
           ProjectCompleteness.TryGetValue(projectFilePath, out var isComplete) &&
           isComplete;
}
