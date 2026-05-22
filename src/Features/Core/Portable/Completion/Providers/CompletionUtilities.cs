// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    /// <summary>
    /// Property key for the identifier end position at trigger time.
    /// Used by <see cref="GetCurrentSpanEnd"/> to detect characters typed after the trigger.
    /// </summary>
    private const string OriginalIdentifierEnd = nameof(OriginalIdentifierEnd);

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
    /// Stores the identifier end position at <paramref name="position"/> as a property on <paramref name="item"/>.
    /// </summary>
    public static CompletionItem SetOriginalIdentifierEnd(CompletionItem item, int position, SourceText text, ISyntaxFactsService syntaxFacts)
    {
        var property = GetOriginalIdentifierEndProperty(position, text, syntaxFacts);
        return item.AddProperty(property.Key, property.Value);
    }

    /// <summary>
    /// Returns a property key-value pair for the identifier end position at <paramref name="position"/>.
    /// </summary>
    public static KeyValuePair<string, string> GetOriginalIdentifierEndProperty(int position, SourceText text, ISyntaxFactsService syntaxFacts)
        => KeyValuePair.Create(OriginalIdentifierEnd, ScanForwardThroughIdentifier(position, text, syntaxFacts).ToString());

    private static int ScanForwardThroughIdentifier(int start, SourceText text, ISyntaxFactsService syntaxFacts)
    {
        var end = start;
        while (end < text.Length && syntaxFacts.IsIdentifierPartCharacter(text[end]))
        {
            end++;
        }

        return end;
    }

    /// <summary>
    /// Returns <c>item.Span.End</c> adjusted forward by the number of identifier characters
    /// typed since the completion session started. When <c>GetChangeAsync</c> receives the
    /// trigger-time document (CommitManager path), this returns <c>item.Span.End</c> unchanged.
    /// When it receives the current document (LSP path), extra typed characters are detected.
    /// </summary>
    public static int GetCurrentSpanEnd(CompletionItem item, SourceText text, ISyntaxFactsService syntaxFacts)
    {
        var spanEnd = item.Span.End;

        if (item.TryGetProperty(OriginalIdentifierEnd, out var endStr)
            && int.TryParse(endStr, out var originalIdentifierEnd))
        {
            var currentIdentifierEnd = ScanForwardThroughIdentifier(item.Span.Start, text, syntaxFacts);
            var typedChars = Math.Max(0, currentIdentifierEnd - originalIdentifierEnd);

            spanEnd += typedChars;
        }

        return spanEnd;
    }
}
