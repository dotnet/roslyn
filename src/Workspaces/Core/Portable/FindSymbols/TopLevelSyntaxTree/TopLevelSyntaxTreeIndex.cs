// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class TopLevelSyntaxTreeIndex : AbstractSyntaxIndex<TopLevelSyntaxTreeIndex>
{
    private readonly DeclarationInfo _declarationInfo;
    private readonly ExtensionMethodInfo _extensionMethodInfo;

    private readonly Lazy<HashSet<DeclaredSymbolInfo>> _declaredSymbolInfoSet;

    private TopLevelSyntaxTreeIndex(
        Checksum? checksum,
        DeclarationInfo declarationInfo,
        ExtensionMethodInfo extensionMethodInfo)
        : base(checksum)
    {
        _declarationInfo = declarationInfo;
        _extensionMethodInfo = extensionMethodInfo;

        _declaredSymbolInfoSet = new(() => [.. this.DeclaredSymbolInfos]);
    }

    public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos => _declarationInfo.DeclaredSymbolInfos;

    /// <summary>
    /// Same as <see cref="DeclaredSymbolInfos"/>, just stored as a set for easy containment checks.
    /// </summary>
    public HashSet<DeclaredSymbolInfo> DeclaredSymbolInfoSet => _declaredSymbolInfoSet.Value;

    public ImmutableDictionary<string, ImmutableArray<int>> ReceiverTypeNameToExtensionMethodMap
        => _extensionMethodInfo.ReceiverTypeNameToExtensionMethodMap;

    public bool ContainsExtensionMethod
        => _extensionMethodInfo.ContainsExtensionMethod;

    public static ValueTask<TopLevelSyntaxTreeIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, cancellationToken);

    public static ValueTask<TopLevelSyntaxTreeIndex> GetRequiredIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(solutionKey, project, document, ReadIndex, CreateIndex, cancellationToken);

    public static ValueTask<TopLevelSyntaxTreeIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, cancellationToken);

    public static ValueTask<TopLevelSyntaxTreeIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, ReadIndex, CreateIndex, cancellationToken);

    public static ValueTask<TopLevelSyntaxTreeIndex?> GetIndexAsync(Document document, bool loadOnly, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, loadOnly, cancellationToken);

    public static ValueTask<TopLevelSyntaxTreeIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, bool loadOnly, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, loadOnly, ReadIndex, CreateIndex, cancellationToken);
}
