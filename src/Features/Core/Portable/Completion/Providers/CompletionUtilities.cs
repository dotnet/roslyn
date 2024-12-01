// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static class CompletionUtilities
{
    public static bool IsTypeImplicitlyConvertible(Compilation compilation, ITypeSymbol sourceType, ImmutableArray<ITypeSymbol> targetTypes)
    {
        foreach (var targetType in targetTypes)
        {
            if (compilation.ClassifyCommonConversion(sourceType, targetType).IsImplicit)
            {
                return true;
            }
        }

        return false;
    }

    public static ImmutableArray<Project> GetDistinctProjectsFromLatestSolutionSnapshot(ImmutableSegmentedList<Project> projects)
    {
        if (projects.IsEmpty)
            return [];

        Solution? solution = null;
        using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectIds);

        // Use WorkspaceVersion to decide which solution snapshot is latest among projects in list.
        // Dedupe and return corresponding projects from this snapshot.
        foreach (var project in projects)
        {
            projectIds.Add(project.Id);
            if (solution is null || project.Solution.WorkspaceVersion > solution.WorkspaceVersion)
            {
                solution = project.Solution;
            }
        }

        Contract.ThrowIfNull(solution);
        return projectIds.Select(solution.GetProject).WhereNotNull().ToImmutableArray();
    }
}
