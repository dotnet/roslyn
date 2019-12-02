// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics
{
    /// <summary>
    /// We have this builder to avoid creating collections unnecessarily.
    /// Expectation is that, most of time, most of analyzers doesn't have any diagnostics. so no need to actually create any objects.
    /// </summary>
    internal struct DiagnosticAnalysisResultBuilder
    {
        public readonly Project Project;
        public readonly VersionStamp Version;

        private HashSet<DocumentId>? _lazyDocumentsWithDiagnostics;

        private Dictionary<DocumentId, List<DiagnosticData>>? _lazySyntaxLocals;
        private Dictionary<DocumentId, List<DiagnosticData>>? _lazySemanticLocals;
        private Dictionary<DocumentId, List<DiagnosticData>>? _lazyNonLocals;

        private List<DiagnosticData>? _lazyOthers;

        public DiagnosticAnalysisResultBuilder(Project project, VersionStamp version)
        {
            Project = project;
            Version = version;

            _lazyDocumentsWithDiagnostics = null;
            _lazySyntaxLocals = null;
            _lazySemanticLocals = null;
            _lazyNonLocals = null;
            _lazyOthers = null;
        }

        public ImmutableHashSet<DocumentId> DocumentIds => _lazyDocumentsWithDiagnostics == null ? ImmutableHashSet<DocumentId>.Empty : _lazyDocumentsWithDiagnostics.ToImmutableHashSet();
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SyntaxLocals => Convert(_lazySyntaxLocals);
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SemanticLocals => Convert(_lazySemanticLocals);
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> NonLocals => Convert(_lazyNonLocals);
        public ImmutableArray<DiagnosticData> Others => _lazyOthers == null ? ImmutableArray<DiagnosticData>.Empty : _lazyOthers.ToImmutableArray();

        public void AddExternalSyntaxDiagnostics(DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
        {
            // this is for diagnostic producer that doesnt use compiler based DiagnosticAnalyzer such as TypeScript.
            AddExternalDiagnostics(ref _lazySyntaxLocals, documentId, diagnostics);
        }

        public void AddExternalSemanticDiagnostics(DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
        {
            // this is for diagnostic producer that doesnt use compiler based DiagnosticAnalyzer such as TypeScript.
            AddExternalDiagnostics(ref _lazySemanticLocals, documentId, diagnostics);
        }

        private void AddExternalDiagnostics(
            ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
        {
            Contract.ThrowIfTrue(Project.SupportsCompilation);

            foreach (var diagnostic in diagnostics)
            {
                // REVIEW: what is our plan for additional locations? 
                switch (diagnostic.Location.Kind)
                {
                    case LocationKind.ExternalFile:
                        {
                            var diagnosticDocumentId = GetExternalDocumentId(Project, diagnostic);
                            if (documentId == diagnosticDocumentId)
                            {
                                // local diagnostics to a file
                                AddDocumentDiagnostic(ref lazyLocals, Project.GetDocument(diagnosticDocumentId), diagnostic);
                            }
                            else if (diagnosticDocumentId != null)
                            {
                                // non local diagnostics to a file
                                AddDocumentDiagnostic(ref _lazyNonLocals, Project.GetDocument(diagnosticDocumentId), diagnostic);
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

        private void AddDocumentDiagnostic(ref Dictionary<DocumentId, List<DiagnosticData>>? map, Document? document, Diagnostic diagnostic)
        {
            if (document is null || !document.SupportsDiagnostics())
            {
                return;
            }

            map ??= new Dictionary<DocumentId, List<DiagnosticData>>();
            map.GetOrAdd(document.Id, _ => new List<DiagnosticData>()).Add(DiagnosticData.Create(diagnostic, document));

            _lazyDocumentsWithDiagnostics ??= new HashSet<DocumentId>();
            _lazyDocumentsWithDiagnostics.Add(document.Id);
        }

        private void AddOtherDiagnostic(DiagnosticData data)
        {
            _lazyOthers ??= new List<DiagnosticData>();
            _lazyOthers.Add(data);
        }

        public void AddSyntaxDiagnostics(SyntaxTree tree, IEnumerable<Diagnostic> diagnostics)
        {
            AddDiagnostics(ref _lazySyntaxLocals, tree, diagnostics);
        }

        public void AddSemanticDiagnostics(SyntaxTree tree, IEnumerable<Diagnostic> diagnostics)
        {
            AddDiagnostics(ref _lazySemanticLocals, tree, diagnostics);
        }

        public void AddCompilationDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            Dictionary<DocumentId, List<DiagnosticData>>? dummy = null;
            AddDiagnostics(ref dummy, tree: null, diagnostics: diagnostics);

            // dummy should be always null since tree is null
            Debug.Assert(dummy == null);
        }

        private void AddDiagnostics(
            ref Dictionary<DocumentId, List<DiagnosticData>>? lazyLocals, SyntaxTree? tree, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                // REVIEW: what is our plan for additional locations? 
                switch (diagnostic.Location.Kind)
                {
                    case LocationKind.ExternalFile:
                        // TODO: currently additional file location is not supported.
                        break;

                    case LocationKind.None:
                        AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
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
                            AddOtherDiagnostic(DiagnosticData.Create(diagnostic, Project));
                        }

                        break;

                    case LocationKind.MetadataFile:
                    case LocationKind.XmlFile:
                        // ignore
                        continue;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(diagnostic.Location.Kind);
                }
            }
        }

        private static DocumentId GetExternalDocumentId(Project project, Diagnostic diagnostic)
        {
            var projectId = project.Id;
            var lineSpan = diagnostic.Location.GetLineSpan();

            return project.Solution.GetDocumentIdsWithFilePath(lineSpan.Path).FirstOrDefault(id => id.ProjectId == projectId);
        }

        private static ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> Convert(Dictionary<DocumentId, List<DiagnosticData>>? map)
        {
            return map == null ?
                ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty :
                map.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableArray());
        }
    }
}
