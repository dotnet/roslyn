// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
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

        private HashSet<DocumentId> _lazySet;

        private Dictionary<DocumentId, List<DiagnosticData>> _lazySyntaxLocals;
        private Dictionary<DocumentId, List<DiagnosticData>> _lazySemanticLocals;
        private Dictionary<DocumentId, List<DiagnosticData>> _lazyNonLocals;

        private List<DiagnosticData> _lazyOthers;

        public DiagnosticAnalysisResultBuilder(Project project, VersionStamp version)
        {
            Project = project;
            Version = version;

            _lazySet = null;
            _lazySyntaxLocals = null;
            _lazySemanticLocals = null;
            _lazyNonLocals = null;
            _lazyOthers = null;
        }

        public ImmutableHashSet<DocumentId> DocumentIds => _lazySet == null ? ImmutableHashSet<DocumentId>.Empty : _lazySet.ToImmutableHashSet();
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
            ref Dictionary<DocumentId, List<DiagnosticData>> lazyLocals, DocumentId documentId, IEnumerable<Diagnostic> diagnostics)
        {
            Contract.ThrowIfTrue(Project.SupportsCompilation);

            var workspace = Project.Solution.Workspace;

            foreach (var diagnostic in diagnostics)
            {
                // REVIEW: what is our plan for additional locations? 
                switch (diagnostic.Location.Kind)
                {
                    case LocationKind.ExternalFile:
                        {
                            var diagnosticDocumentId = GetExternalDocumentId(diagnostic);
                            if (documentId == diagnosticDocumentId)
                            {
                                // local diagnostics to a file
                                AppendDiagnostics(ref lazyLocals, Project.GetDocument(diagnosticDocumentId), diagnostic);
                            }
                            else if (diagnosticDocumentId != null)
                            {
                                // non local diagnostics to a file
                                AppendDiagnostics(ref _lazyNonLocals, Project.GetDocument(diagnosticDocumentId), diagnostic);
                            }
                            else
                            {
                                // non local diagnostics without location
                                _lazyOthers ??= new List<DiagnosticData>();
                                _lazyOthers.Add(DiagnosticData.Create(workspace, diagnostic, Project.Id));
                            }

                            break;
                        }
                    case LocationKind.None:
                        {
                            _lazyOthers ??= new List<DiagnosticData>();
                            _lazyOthers.Add(DiagnosticData.Create(workspace, diagnostic, Project.Id));
                            break;
                        }
                    case LocationKind.SourceFile:
                    case LocationKind.MetadataFile:
                    case LocationKind.XmlFile:
                        {
                            // something we don't care
                            continue;
                        }
                    default:
                        {
                            Contract.Fail("should not reach");
                            break;
                        }
                }
            }
        }

        private void AppendDiagnostics(ref Dictionary<DocumentId, List<DiagnosticData>> map, Document documentOpt, Diagnostic diagnostic)
        {
            if (documentOpt is null)
            {
                return;
            }

            if (!documentOpt.SupportsDiagnostics())
            {
                return;
            }

            map ??= new Dictionary<DocumentId, List<DiagnosticData>>();
            map.GetOrAdd(documentOpt.Id, _ => new List<DiagnosticData>()).Add(DiagnosticData.Create(documentOpt, diagnostic));

            AddDocumentToSet(documentOpt);
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
            Dictionary<DocumentId, List<DiagnosticData>> dummy = null;
            AddDiagnostics(ref dummy, tree: null, diagnostics: diagnostics);

            // dummy should be always null
            Debug.Assert(dummy == null);
        }

        private void AddDiagnostics(
            ref Dictionary<DocumentId, List<DiagnosticData>> lazyLocals, SyntaxTree tree, IEnumerable<Diagnostic> diagnostics)
        {
            var workspace = Project.Solution.Workspace;

            foreach (var diagnostic in diagnostics)
            {
                // REVIEW: what is our plan for additional locations? 
                switch (diagnostic.Location.Kind)
                {
                    case LocationKind.ExternalFile:
                        {
                            // TODO: currently additional file location is not supported.
                            break;
                        }
                    case LocationKind.None:
                        {
                            _lazyOthers ??= new List<DiagnosticData>();
                            _lazyOthers.Add(DiagnosticData.Create(workspace, diagnostic, Project.Id));
                            break;
                        }
                    case LocationKind.SourceFile:
                        {
                            if (tree != null && diagnostic.Location.SourceTree == tree)
                            {
                                // local diagnostics to a file
                                AppendDiagnostics(ref lazyLocals, GetDocument(diagnostic), diagnostic);
                            }
                            else if (diagnostic.Location.SourceTree != null)
                            {
                                // non local diagnostics to a file
                                AppendDiagnostics(ref _lazyNonLocals, Project.GetDocument(diagnostic.Location.SourceTree), diagnostic);
                            }
                            else
                            {
                                // non local diagnostics without location
                                _lazyOthers ??= new List<DiagnosticData>();
                                _lazyOthers.Add(DiagnosticData.Create(workspace, diagnostic, Project.Id));
                            }

                            break;
                        }
                    case LocationKind.MetadataFile:
                    case LocationKind.XmlFile:
                        {
                            // something we don't care
                            continue;
                        }
                    default:
                        {
                            Contract.Fail("should not reach");
                            break;
                        }
                }
            }
        }

        private void AddDocumentToSet(Document document)
        {
            _lazySet ??= new HashSet<DocumentId>();
            _lazySet.Add(document.Id);
        }

        private Document GetDocument(Diagnostic diagnostic)
        {
            return Project.GetDocument(diagnostic.Location.SourceTree);
        }

        private DocumentId GetExternalDocumentId(Diagnostic diagnostic)
        {
            var projectId = Project.Id;
            var lineSpan = diagnostic.Location.GetLineSpan();

            return Project.Solution.GetDocumentIdsWithFilePath(lineSpan.Path).FirstOrDefault(id => id.ProjectId == projectId);
        }

        private static ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> Convert(Dictionary<DocumentId, List<DiagnosticData>> map)
        {
            return map == null ?
                ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty :
                map.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableArray());
        }
    }
}
