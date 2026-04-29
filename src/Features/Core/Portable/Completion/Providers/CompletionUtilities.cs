// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static class CompletionUtilities
{
    public static bool IsTypeImplicitlyConvertible(Compilation compilation, ITypeSymbol sourceType, IEnumerable<ITypeSymbol> targetTypes)
    {
        foreach (var targetType in targetTypes)
        {
            if (compilation.ClassifyCommonConversion(sourceType, targetType).IsImplicit)
                return true;
        }

        return false;
    }

    public static bool IsTypeImplicitlyConvertible(Compilation compilation, ITypeSymbol sourceType, ImmutableArray<ITypeSymbol> targetTypes)
    {
        foreach (var targetType in targetTypes)
        {
            if (compilation.ClassifyCommonConversion(sourceType, targetType).IsImplicit)
                return true;
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
            if (solution is null || project.Solution.SolutionStateContentVersion > solution.SolutionStateContentVersion)
            {
                solution = project.Solution;
            }
        }

        Contract.ThrowIfNull(solution);
        return [.. projectIds.Select(solution.GetProject).WhereNotNull()];
    }

    /// <summary>
    /// Finds the end of any identifier characters that the user has typed starting at
    /// <paramref name="start"/>. <see cref="CompletionItem.Span"/> is frozen at session start
    /// and does not advance as the user types. This method scans forward to find the actual end
    /// of the typed text so the replacement span covers all characters entered since the trigger.
    /// </summary>
    public static int GetCurrentSpanEnd(int start, SourceText text, ISyntaxFactsService syntaxFacts)
    {
        var end = start;
        while (end < text.Length && syntaxFacts.IsIdentifierPartCharacter(text[end]))
        {
            end++;
        }

        return end;
    }
}
