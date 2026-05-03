// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics;

/// <summary>
/// We have this builder to avoid creating collections unnecessarily.
/// Expectation is that, most of time, most of analyzers doesn't have any diagnostics. so no need to actually create any objects.
/// </summary>
internal struct DiagnosticAnalysisResultBuilder(Project project)
{
    public readonly Project Project = project;

    private Dictionary<DocumentId, List<DiagnosticData>>? _lazySyntaxLocals = null;
    private Dictionary<DocumentId, List<DiagnosticData>>? _lazySemanticLocals = null;
    private Dictionary<DocumentId, List<DiagnosticData>>? _lazyNonLocals = null;

    private List<DiagnosticData>? _lazyOthers = null;

    public readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SyntaxLocals => Convert(_lazySyntaxLocals);
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SemanticLocals => Convert(_lazySemanticLocals);
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> NonLocals => Convert(_lazyNonLocals);
    public readonly ImmutableArray<DiagnosticData> Others => _lazyOthers == null ? [] : [.. _lazyOthers];

    public void AddExternalSyntaxDiagnostics(DocumentId documentId, ImmutableArray<Diagnostic> diagnostics)
    {
        AddExternalDiagnostics(ref _lazySyntaxLocals, documentId, diagnostics);
    }

    public void AddExternalSemanticDiagnostics(DocumentId documentId, ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.Length == 0)
            return;

        AddExternalDiagnostics(ref _lazySemanticLocals, documentId, diagnostics);
    }

    private void AddExternalDiagnostics(
        ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, DocumentId documentId, ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            // REVIEW: what is our plan for additional locations? 
            switch (diagnostic.Location.Kind)
            {
                case LocationKind.ExternalFile:
                    {
                        var diagnosticDocumentId = Project.GetDocumentForExternalLocation(diagnostic.Location);
                        if (documentId == diagnosticDocumentId)
                        {
                            // local diagnostics to a file
                            AddDocumentDiagnostic(ref lazyLocals, Project.GetRequiredTextDocument(diagnosticDocumentId), diagnostic);
                        }
                        else if (diagnosticDocumentId != null)
                        {
                            // non local diagnostics to a file
                            AddDocumentDiagnostic(ref _lazyNonLocals, Project.GetRequiredTextDocument(diagnosticDocumentId), diagnostic);
                        }
                        else
                        {
                            // non local diagnostics without location
                            AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
                        }

                        break;
                    }

                case LocationKind.None:
                    AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
                    break;

                case LocationKind.SourceFile:
                case LocationKind.MetadataFile:
                case LocationKind.XmlFile:
                    // ignore
                    continue;

                case var kind:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }

    private static void AddDocumentDiagnostic(ref Dictionary<DocumentId, List<DiagnosticData>>? map, TextDocument document, Diagnostic diagnostic)
    {
        if (!document.SupportsDiagnostics())
            return;

        map ??= [];
        map.GetOrAdd(document.Id, static _ => []).Add(DiagnosticData.Create(diagnostic, document));
    }

    private void AddOtherDiagnostic(DiagnosticData data)
    {
        _lazyOthers ??= [];
        _lazyOthers.Add(data);
    }

    public void AddSyntaxDiagnostics(SyntaxTree tree, ImmutableArray<Diagnostic> diagnostics)
        => AddDiagnostics(ref _lazySyntaxLocals, tree, diagnostics);

    public void AddDiagnosticTreatedAsLocalSemantic(Diagnostic diagnostic)
        => AddDiagnostic(ref _lazySemanticLocals, diagnostic.Location.SourceTree, diagnostic);

    public void AddSemanticDiagnostics(SyntaxTree tree, ImmutableArray<Diagnostic> diagnostics)
        => AddDiagnostics(ref _lazySemanticLocals, tree, diagnostics);

    public void AddCompilationDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        Dictionary<DocumentId, List<DiagnosticData>>? dummy = null;
        AddDiagnostics(ref dummy, tree: null, diagnostics: diagnostics);

        // dummy should be always null since tree is null
        Debug.Assert(dummy == null);
    }

    private void AddDiagnostic(
        ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, SyntaxTree? tree, Diagnostic diagnostic)
    {
        // REVIEW: what is our plan for additional locations? 
        switch (diagnostic.Location.Kind)
        {
            case LocationKind.ExternalFile:
                var diagnosticDocumentId = Project.GetDocumentForExternalLocation(diagnostic.Location);
                if (diagnosticDocumentId != null)
                {
                    AddDocumentDiagnostic(ref _lazyNonLocals, Project.GetRequiredTextDocument(diagnosticDocumentId), diagnostic);
                }
                else
                {
                    AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
                }

                break;

            case LocationKind.None:
                AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
                break;

            case LocationKind.SourceFile:
                var diagnosticTree = diagnostic.Location.SourceTree;
                if (tree != null && diagnosticTree == tree)
                {
                    // local diagnostics to a file
                    AddDocumentDiagnostic(ref lazyLocals, Project.GetRequiredDocument(diagnosticTree), diagnostic);
                }
                else if (diagnosticTree != null)
                {
                    // non local diagnostics to a file
                    AddDocumentDiagnostic(ref _lazyNonLocals, Project.GetRequiredDocument(diagnosticTree), diagnostic);
                }
                else
                {
                    // non local diagnostics without location
                    AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
                }

                break;

            case LocationKind.MetadataFile:
            case LocationKind.XmlFile:
                // ignore
                return;

            default:
                throw ExceptionUtilities.UnexpectedValue(diagnostic.Location.Kind);
        }
    }

    private void AddDiagnostics(
        ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, SyntaxTree? tree, ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            AddDiagnostic(ref lazyLocals, tree, diagnostic);
    }

    private static ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> Convert(Dictionary<DocumentId, List<DiagnosticData>>? map)
    {
        return map == null
            ? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty
            : map.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableArray());
    }
}
