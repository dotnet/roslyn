// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics;

/// <summary>
/// We have this builder to avoid creating collections unnecessarily.
/// Expectation is that, most of time, most of analyzers doesn't have any diagnostics. so no need to actually create any objects.
/// </summary>
internal struct DiagnosticAnalysisResultBuilder(Project project, VersionStamp version)
{
    public readonly Project Project = project;
    public readonly VersionStamp Version = version;

    private HashSet<DocumentId>? _lazyDocumentsWithDiagnostics = null;

    private Dictionary<DocumentId, List<DiagnosticData>>? _lazySyntaxLocals = null;
    private Dictionary<DocumentId, List<DiagnosticData>>? _lazySemanticLocals = null;
    private Dictionary<DocumentId, List<DiagnosticData>>? _lazyNonLocals = null;

    private List<DiagnosticData>? _lazyOthers = null;

    public readonly ImmutableHashSet<DocumentId> DocumentIds => _lazyDocumentsWithDiagnostics == null ? [] : _lazyDocumentsWithDiagnostics.ToImmutableHashSet();
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SyntaxLocals => Convert(_lazySyntaxLocals);
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SemanticLocals => Convert(_lazySemanticLocals);
    public readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> NonLocals => Convert(_lazyNonLocals);
    public readonly ImmutableArray<DiagnosticData> Others => _lazyOthers == null ? [] : _lazyOthers.ToImmutableArray();

    public void AddExternalSyntaxDiagnostics(DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
    {
        AddExternalDiagnostics(ref _lazySyntaxLocals, documentId, diagnostics);
    }

    public void AddExternalSemanticDiagnostics(DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
    {
        // this is for diagnostic producer that doesnt use compiler based DiagnosticAnalyzer such as TypeScript.
        Contract.ThrowIfTrue(Project.SupportsCompilation);

        AddExternalDiagnostics(ref _lazySemanticLocals, documentId, diagnostics);
    }

    private void AddExternalDiagnostics(
        ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
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
                            AddDocumentDiagnostic(ref lazyLocals, Project.GetTextDocument(diagnosticDocumentId), diagnostic);
                        }
                        else if (diagnosticDocumentId != null)
                        {
                            // non local diagnostics to a file
                            AddDocumentDiagnostic(ref _lazyNonLocals, Project.GetTextDocument(diagnosticDocumentId), diagnostic);
                        }
                        else
                        {
                            // non local diagnostics without location
                            AddOtherDiagnostic(DiagnosticData.Create(Project.Solution, diagnostic, Project));
                        }

                        break;
                    }

                case LocationKind.None:
                    AddOtherDiagnostic(DiagnosticData.Create(Project.Solution, diagnostic, Project));
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

    private void AddDocumentDiagnostic(ref Dictionary<DocumentId, List<DiagnosticData>>? map, TextDocument? document, Diagnostic diagnostic)
    {
        if (document is null || !document.SupportsDiagnostics())
        {
            return;
        }

        map ??= [];
        map.GetOrAdd(document.Id, _ => []).Add(DiagnosticData.Create(diagnostic, document));

        _lazyDocumentsWithDiagnostics ??= [];
        _lazyDocumentsWithDiagnostics.Add(document.Id);
    }

    private void AddOtherDiagnostic(DiagnosticData data)
    {
        _lazyOthers ??= [];
        _lazyOthers.Add(data);
    }

    public void AddSyntaxDiagnostics(SyntaxTree tree, IEnumerable<Diagnostic> diagnostics)
        => AddDiagnostics(ref _lazySyntaxLocals, tree, diagnostics);

    public void AddDiagnosticTreatedAsLocalSemantic(Diagnostic diagnostic)
        => AddDiagnostic(ref _lazySemanticLocals, diagnostic.Location.SourceTree, diagnostic);

    public void AddSemanticDiagnostics(SyntaxTree tree, IEnumerable<Diagnostic> diagnostics)
        => AddDiagnostics(ref _lazySemanticLocals, tree, diagnostics);

    public void AddCompilationDiagnostics(IEnumerable<Diagnostic> diagnostics)
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
                    AddOtherDiagnostic(DiagnosticData.Create(Project.Solution, diagnostic, Project));
                }

                break;

            case LocationKind.None:
                AddOtherDiagnostic(DiagnosticData.Create(Project.Solution, diagnostic, Project));
                break;

            case LocationKind.SourceFile:
                var diagnosticTree = diagnostic.Location.SourceTree;
                if (tree != null && diagnosticTree == tree)
                {
                    // local diagnostics to a file
                    AddDocumentDiagnostic(ref lazyLocals, Project.GetDocument(diagnosticTree), diagnostic);
                }
                else if (diagnosticTree != null)
                {
                    // non local diagnostics to a file
                    AddDocumentDiagnostic(ref _lazyNonLocals, Project.GetDocument(diagnosticTree), diagnostic);
                }
                else
                {
                    // non local diagnostics without location
                    AddOtherDiagnostic(DiagnosticData.Create(Project.Solution, diagnostic, Project));
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
        ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, SyntaxTree? tree, IEnumerable<Diagnostic> diagnostics)
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
