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
    /// Returns the <see cref="PatternMatcherKind"/> flags indicating which matching strategies are
    /// worth attempting for this document. Returns <see cref="PatternMatcherKind.None"/> if the
    /// document can be skipped entirely. Used by NavigateTo to skip documents early.
    /// </summary>
    internal PatternMatcherKind CouldContainNavigateToMatch(string patternName, string? patternContainer)
        => _navigateToSearchInfo.CouldContainNavigateToMatch(patternName, patternContainer);

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
        public bool HumpCheckPasses(string patternName)
            => index._navigateToSearchInfo.HumpCheckPasses(patternName);

        public bool TrigramCheckPasses(string patternName)
            => index._navigateToSearchInfo.TrigramCheckPasses(patternName);

        public bool LengthCheckPasses(string patternName)
            => index._navigateToSearchInfo.LengthCheckPasses(patternName);

        public bool BigramCountCheckPasses(string patternName)
            => index._navigateToSearchInfo.BigramCountCheckPasses(patternName);

        public bool ContainerCheckPasses(string patternContainer)
            => index._navigateToSearchInfo.ContainerCheckPasses(patternContainer);

        public static NavigateToSearchIndex CreateIndex(ImmutableArray<DeclaredSymbolInfo> infos)
        {
            return new NavigateToSearchIndex(
                checksum: null,
                NavigateToSearchInfo.Create(infos));
        }
    }
}
