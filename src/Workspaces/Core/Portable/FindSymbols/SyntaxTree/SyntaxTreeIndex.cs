// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class SyntaxTreeIndex : AbstractSyntaxIndex<SyntaxTreeIndex>
{
    private readonly LiteralInfo _literalInfo;
    private readonly IdentifierInfo _identifierInfo;
    private readonly ContextInfo _contextInfo;
    private readonly HashSet<(string alias, string name, int arity)>? _globalAliasInfo;

    private SyntaxTreeIndex(
        Checksum? checksum,
        LiteralInfo literalInfo,
        IdentifierInfo identifierInfo,
        ContextInfo contextInfo,
        HashSet<(string alias, string name, int arity)>? globalAliasInfo)
        : base(checksum)
    {
        _literalInfo = literalInfo;
        _identifierInfo = identifierInfo;
        _contextInfo = contextInfo;
        _globalAliasInfo = globalAliasInfo;
    }

    public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(Document document, IChecksummedPersistentStorage storage, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, storage, cancellationToken);

    public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, IChecksummedPersistentStorage storage, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(solutionKey, project, document, ReadIndex, CreateIndex, storage, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, IChecksummedPersistentStorage storage, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, storage, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, IChecksummedPersistentStorage storage, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, loadOnly: false, storage, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, bool loadOnly, IChecksummedPersistentStorage storage, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, loadOnly, storage, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, bool loadOnly, IChecksummedPersistentStorage storage, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, loadOnly, ReadIndex, CreateIndex, storage, cancellationToken);
}
