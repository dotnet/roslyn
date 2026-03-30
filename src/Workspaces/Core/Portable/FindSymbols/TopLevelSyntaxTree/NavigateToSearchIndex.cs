// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// A lightweight index containing only the pre-computed filter data needed for NavigateTo to quickly
/// skip documents that cannot match a search pattern.  This is stored and loaded separately from
/// <see cref="TopLevelSyntaxTreeIndex"/> so that the (much larger) declared-symbol data need not be
/// loaded for documents that the filter rejects.
/// </summary>
internal sealed partial class NavigateToSearchIndex : AbstractSyntaxIndex<NavigateToSearchIndex>
{
    private readonly NavigateToSearchInfo _navigateToSearchInfo;

    private NavigateToSearchIndex(
        Checksum? checksum,
        NavigateToSearchInfo navigateToSearchInfo)
        : base(checksum)
    {
        _navigateToSearchInfo = navigateToSearchInfo;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this document probably contains at least one symbol whose
    /// name matches <paramref name="patternName"/> and (if specified) whose container matches
    /// <paramref name="patternContainer"/>. Used by NavigateTo to skip documents early.
    /// <paramref name="allowFuzzyMatching"/> indicates which matching modes are worth attempting.
    /// </summary>
    internal bool CouldContainNavigateToMatch(string patternName, string? patternContainer, out bool allowFuzzyMatching)
        => _navigateToSearchInfo.ProbablyContainsMatch(patternName, patternContainer, out allowFuzzyMatching);

    public static ValueTask<NavigateToSearchIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, cancellationToken);

    public static ValueTask<NavigateToSearchIndex> GetRequiredIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(solutionKey, project, document, ReadIndex, CreateIndex, cancellationToken);

    public static ValueTask<NavigateToSearchIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, cancellationToken);

    public static ValueTask<NavigateToSearchIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, ReadIndex, CreateIndex, cancellationToken);

    public static ValueTask<NavigateToSearchIndex?> GetIndexAsync(Document document, bool loadOnly, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, loadOnly, cancellationToken);

    public static ValueTask<NavigateToSearchIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, bool loadOnly, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, loadOnly, ReadIndex, CreateIndex, cancellationToken);

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(NavigateToSearchIndex index)
    {
        public bool HumpCheckProbablyMatches(string patternName)
            => index._navigateToSearchInfo.HumpCheckPasses(patternName);

        public bool TrigramCheckProbablyMatches(string patternName)
            => index._navigateToSearchInfo.TrigramCheckPasses(patternName);

        public bool LengthCheckProbablyMatches(string patternName)
            => index._navigateToSearchInfo.LengthCheckPasses(patternName);

        public bool ContainerCheckProbablyMatches(string patternContainer)
            => index._navigateToSearchInfo.ContainerProbablyMatches(patternContainer);

        public static NavigateToSearchIndex CreateIndex(ImmutableArray<DeclaredSymbolInfo> infos)
        {
            return new NavigateToSearchIndex(
                checksum: null,
                NavigateToSearchInfo.Create(infos));
        }
    }
}
