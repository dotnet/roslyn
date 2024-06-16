// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class SyntaxTreeIndex : AbstractSyntaxIndex<SyntaxTreeIndex>
{
    private readonly LiteralInfo _literalInfo;
    private readonly IdentifierInfo _identifierInfo;
    private readonly ContextInfo _contextInfo;
    private readonly HashSet<(string alias, string name, int arity, bool isGlobal)>? _aliasInfo;
    private readonly Dictionary<InterceptsLocationData, TextSpan>? _interceptsLocationInfo;

    private SyntaxTreeIndex(
        Checksum? checksum,
        LiteralInfo literalInfo,
        IdentifierInfo identifierInfo,
        ContextInfo contextInfo,
        HashSet<(string alias, string name, int arity, bool isGlobal)>? aliasInfo,
        Dictionary<InterceptsLocationData, TextSpan>? interceptsLocationInfo)
        : base(checksum)
    {
        _literalInfo = literalInfo;
        _identifierInfo = identifierInfo;
        _contextInfo = contextInfo;
        _aliasInfo = aliasInfo;
        _interceptsLocationInfo = interceptsLocationInfo;
    }

    public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, cancellationToken);

    public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, CancellationToken cancellationToken)
        => GetRequiredIndexAsync(solutionKey, project, document, ReadIndex, CreateIndex, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, loadOnly: false, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, bool loadOnly, CancellationToken cancellationToken)
        => GetIndexAsync(SolutionKey.ToSolutionKey(document.Project.Solution), document.Project.State, (DocumentState)document.State, loadOnly, cancellationToken);

    public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(SolutionKey solutionKey, ProjectState project, DocumentState document, bool loadOnly, CancellationToken cancellationToken)
        => GetIndexAsync(solutionKey, project, document, loadOnly, ReadIndex, CreateIndex, cancellationToken);
}
