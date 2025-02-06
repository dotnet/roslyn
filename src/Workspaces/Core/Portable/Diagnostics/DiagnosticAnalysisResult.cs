// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics;

/// <summary>
/// This holds onto diagnostics for a specific version of project snapshot
/// in a way each kind of diagnostics can be queried fast.
/// </summary>
internal readonly struct DiagnosticAnalysisResult
{
    public readonly ProjectId ProjectId;
    public readonly VersionStamp Version;

    /// <summary>
    /// The set of documents that has any kind of diagnostics on it.
    /// </summary>
    public readonly ImmutableHashSet<DocumentId>? DocumentIds;
    public readonly bool IsEmpty;

    /// <summary>
    /// Syntax diagnostics from this file.
    /// </summary>
    private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? _syntaxLocals;

    /// <summary>
    /// Semantic diagnostics from this file.
    /// </summary>
    private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? _semanticLocals;

    /// <summary>
    /// Diagnostics that were produced for these documents, but came from the analysis of other files.
    /// </summary>
    private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? _nonLocals;

    /// <summary>
    /// Diagnostics that don't have locations.
    /// </summary>
    private readonly ImmutableArray<DiagnosticData> _others;

    private DiagnosticAnalysisResult(
        ProjectId projectId,
        VersionStamp version,
        ImmutableHashSet<DocumentId>? documentIds,
        bool isEmpty)
    {
        ProjectId = projectId;
        Version = version;
        DocumentIds = documentIds;
        IsEmpty = isEmpty;

        _syntaxLocals = null;
        _semanticLocals = null;
        _nonLocals = null;
        _others = default;
    }

    private DiagnosticAnalysisResult(
        ProjectId projectId,
        VersionStamp version,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocals,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocals,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocals,
        ImmutableArray<DiagnosticData> others,
        ImmutableHashSet<DocumentId>? documentIds)
    {
        Debug.Assert(!others.IsDefault);
        Debug.Assert(!syntaxLocals.Values.Any(item => item.IsDefault));
        Debug.Assert(!semanticLocals.Values.Any(item => item.IsDefault));
        Debug.Assert(!nonLocals.Values.Any(item => item.IsDefault));

        ProjectId = projectId;
        Version = version;

        _syntaxLocals = syntaxLocals;
        _semanticLocals = semanticLocals;
        _nonLocals = nonLocals;
        _others = others;

        DocumentIds = documentIds ?? GetDocumentIds(syntaxLocals, semanticLocals, nonLocals);
        IsEmpty = DocumentIds.IsEmpty && _others.IsEmpty;
    }

    public static DiagnosticAnalysisResult CreateEmpty(ProjectId projectId, VersionStamp version)
    {
        return new DiagnosticAnalysisResult(
            projectId,
            version,
            documentIds: [],
            syntaxLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
            semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
            nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
            others: []);
    }

    public static DiagnosticAnalysisResult CreateInitialResult(ProjectId projectId)
    {
        return new DiagnosticAnalysisResult(
            projectId,
            version: VersionStamp.Default,
            documentIds: null,
            isEmpty: true);
    }

    public static DiagnosticAnalysisResult Create(
        Project project,
        VersionStamp version,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocalMap,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocalMap,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocalMap,
        ImmutableArray<DiagnosticData> others,
        ImmutableHashSet<DocumentId>? documentIds)
    {
        VerifyDocumentMap(project, syntaxLocalMap);
        VerifyDocumentMap(project, semanticLocalMap);
        VerifyDocumentMap(project, nonLocalMap);

        return new DiagnosticAnalysisResult(
            project.Id,
            version,
            syntaxLocalMap,
            semanticLocalMap,
            nonLocalMap,
            others,
            documentIds);
    }

    public static DiagnosticAnalysisResult CreateFromBuilder(DiagnosticAnalysisResultBuilder builder)
    {
        return Create(
            builder.Project,
            builder.Version,
            builder.SyntaxLocals,
            builder.SemanticLocals,
            builder.NonLocals,
            builder.Others,
            builder.DocumentIds);
    }

    // aggregated form means it has aggregated information but no actual data.
    public bool IsAggregatedForm => _syntaxLocals == null;

    // default analysis result
    public bool IsDefault => DocumentIds == null;

    // make sure we don't return null
    public ImmutableHashSet<DocumentId> DocumentIdsOrEmpty => DocumentIds ?? [];

    private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? GetMap(AnalysisKind kind)
        => kind switch
        {
            AnalysisKind.Syntax => _syntaxLocals,
            AnalysisKind.Semantic => _semanticLocals,
            AnalysisKind.NonLocal => _nonLocals,
            _ => throw ExceptionUtilities.UnexpectedValue(kind)
        };

    public ImmutableArray<DiagnosticData> GetAllDiagnostics()
    {
        // PERF: don't allocation anything if not needed
        if (IsAggregatedForm || IsEmpty)
        {
            return [];
        }

        Contract.ThrowIfNull(_syntaxLocals);
        Contract.ThrowIfNull(_semanticLocals);
        Contract.ThrowIfNull(_nonLocals);
        Contract.ThrowIfTrue(_others.IsDefault);

        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);

        foreach (var data in _syntaxLocals.Values)
            builder.AddRange(data);

        foreach (var data in _semanticLocals.Values)
            builder.AddRange(data);

        foreach (var data in _nonLocals.Values)
            builder.AddRange(data);

        foreach (var data in _others)
            builder.AddRange(data);

        return builder.ToImmutableAndClear();
    }

    public ImmutableArray<DiagnosticData> GetDocumentDiagnostics(DocumentId documentId, AnalysisKind kind)
    {
        if (IsAggregatedForm || IsEmpty)
        {
            return [];
        }

        var map = GetMap(kind);
        Contract.ThrowIfNull(map);

        if (map.TryGetValue(documentId, out var diagnostics))
        {
            Debug.Assert(DocumentIds != null && DocumentIds.Contains(documentId));
            return diagnostics;
        }

        return [];
    }

    public ImmutableArray<DiagnosticData> GetOtherDiagnostics()
        => (IsAggregatedForm || IsEmpty) ? [] : _others;

    public DiagnosticAnalysisResult ToAggregatedForm()
        => new(ProjectId, Version, DocumentIds, IsEmpty);

    public DiagnosticAnalysisResult DropExceptSyntax()
    {
        // quick bail out
        if (_syntaxLocals == null || _syntaxLocals.Count == 0)
        {
            return CreateEmpty(ProjectId, Version);
        }

        // keep only syntax errors
        return new DiagnosticAnalysisResult(
           ProjectId,
           Version,
           _syntaxLocals,
           semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
           nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
           others: [],
           documentIds: null);
    }

    private static ImmutableHashSet<DocumentId> GetDocumentIds(
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? syntaxLocals,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? semanticLocals,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>? nonLocals)
    {
        // quick bail out
        var allEmpty = syntaxLocals ?? semanticLocals ?? nonLocals;
        if (allEmpty == null)
        {
            return [];
        }

        var documents = SpecializedCollections.EmptyEnumerable<DocumentId>();
        if (syntaxLocals != null)
        {
            documents = documents.Concat(syntaxLocals.Keys);
        }

        if (semanticLocals != null)
        {
            documents = documents.Concat(semanticLocals.Keys);
        }

        if (nonLocals != null)
        {
            documents = documents.Concat(nonLocals.Keys);
        }

        return [.. documents];
    }

    [Conditional("DEBUG")]
    private static void VerifyDocumentMap(Project project, ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> map)
    {
        foreach (var documentId in map.Keys)
        {
            // TryGetSourceGeneratedDocumentForAlreadyGeneratedId is being used here for a debug-only assertion. The
            // assertion is claiming that the document in which the diagnostic appears is known to exist in the
            // project. This requires the source generators already have run.
            var textDocument = project.GetTextDocument(documentId) ?? project.TryGetSourceGeneratedDocumentForAlreadyGeneratedId(documentId);
            Debug.Assert(textDocument?.SupportsDiagnostics() == true);
        }
    }
}
