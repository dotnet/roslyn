// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Collections;
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

    /// <summary>
    /// Syntax diagnostics from this file.
    /// </summary>
    private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> _syntaxLocals;

    /// <summary>
    /// Semantic diagnostics from this file.
    /// </summary>
    private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> _semanticLocals;

    /// <summary>
    /// Diagnostics that were produced for these documents, but came from the analysis of other files.
    /// </summary>
    private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> _nonLocals;

    private DiagnosticAnalysisResult(
        ProjectId projectId,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocals,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocals,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocals)
    {
        Debug.Assert(!syntaxLocals.Values.Any(item => item.IsDefault));
        Debug.Assert(!semanticLocals.Values.Any(item => item.IsDefault));
        Debug.Assert(!nonLocals.Values.Any(item => item.IsDefault));

        ProjectId = projectId;

        _syntaxLocals = syntaxLocals;
        _semanticLocals = semanticLocals;
        _nonLocals = nonLocals;
    }

    public static DiagnosticAnalysisResult CreateEmpty(ProjectId projectId)
    {
        return new DiagnosticAnalysisResult(
            projectId,
            syntaxLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
            semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
            nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty);
    }

    public static DiagnosticAnalysisResult Create(
        Project project,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocalMap,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocalMap,
        ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocalMap)
    {
        VerifyDocumentMap(project, syntaxLocalMap);
        VerifyDocumentMap(project, semanticLocalMap);
        VerifyDocumentMap(project, nonLocalMap);

        return new DiagnosticAnalysisResult(
            project.Id,
            syntaxLocalMap,
            semanticLocalMap,
            nonLocalMap);
    }

    public static DiagnosticAnalysisResult CreateFromBuilder(DiagnosticAnalysisResultBuilder builder)
    {
        return Create(
            builder.Project,
            builder.SyntaxLocals,
            builder.SemanticLocals,
            builder.NonLocals);
    }

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
        using var result = TemporaryArray<DiagnosticData>.Empty;

        foreach (var (_, data) in _syntaxLocals)
            result.AddRange(data);

        foreach (var (_, data) in _semanticLocals)
            result.AddRange(data);

        foreach (var (_, data) in _nonLocals)
            result.AddRange(data);

        return result.ToImmutableAndClear();
    }

    public ImmutableArray<DiagnosticData> GetDocumentDiagnostics(DocumentId documentId, AnalysisKind kind)
    {
        var map = GetMap(kind);
        Contract.ThrowIfNull(map);

        return map.TryGetValue(documentId, out var diagnostics) ? diagnostics : [];
    }

    /// <summary>
    /// Returns a new result instance with no diagnostics except syntax.
    /// </summary>
    /// <returns></returns>
    public DiagnosticAnalysisResult DropExceptSyntax()
        => new(
           ProjectId,
           _syntaxLocals,
           semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
           nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty);

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
