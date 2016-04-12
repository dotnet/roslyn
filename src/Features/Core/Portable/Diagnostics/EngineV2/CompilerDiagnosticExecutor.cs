// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Diagnostic Executor that only relies on compiler layer. this might be replaced by new CompilationWithAnalyzer API.
    /// </summary>
    internal static class CompilerDiagnosticExecutor
    {
        public static async Task<ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>> AnalyzeAsync(this CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            // Run all analyzers at once.
            // REVIEW: why there are 2 different cancellation token? one that I can give to constructor and one I can give in to each method?
            // REVIEW: we drop all those allocations for the diagnostics returned. can we avoid this?
            await analyzerDriver.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

            // this is wierd, but now we iterate through each analyzer for each tree to get cached result.
            // REVIEW: no better way to do this?
            var noSpanFilter = default(TextSpan?);
            var analyzers = analyzerDriver.Analyzers;
            var compilation = analyzerDriver.Compilation;

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalysisResult>();
            foreach (var analyzer in analyzers)
            {
                var result = new Builder(project, version);

                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                foreach (var tree in compilation.SyntaxTrees)
                {
                    var model = compilation.GetSemanticModel(tree);

                    var syntax = await analyzerDriver.GetAnalyzerSyntaxDiagnosticsAsync(tree, oneAnalyzers, cancellationToken).ConfigureAwait(false);
                    result.AddSyntaxDiagnostics(tree, syntax);

                    var semantic = await analyzerDriver.GetAnalyzerSemanticDiagnosticsAsync(model, noSpanFilter, oneAnalyzers, cancellationToken).ConfigureAwait(false);
                    result.AddSemanticDiagnostics(tree, semantic);
                }

                var rest = await analyzerDriver.GetAnalyzerCompilationDiagnosticsAsync(oneAnalyzers, cancellationToken).ConfigureAwait(false);
                result.AddCompilationDiagnostics(rest);

                builder.Add(analyzer, result.ToResult());
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// We have this builder to avoid creating collections unnecessarily.
        /// Expectation is that, most of time, most of analyzers doesn't have any diagnostics. so no need to actually create any objects.
        /// </summary>
        internal struct Builder
        {
            private readonly Project _project;
            private readonly VersionStamp _version;

            private HashSet<DocumentId> _lazySet;

            private Dictionary<DocumentId, List<DiagnosticData>> _lazySyntaxLocals;
            private Dictionary<DocumentId, List<DiagnosticData>> _lazySemanticLocals;
            private Dictionary<DocumentId, List<DiagnosticData>> _lazyNonLocals;

            private List<DiagnosticData> _lazyOthers;

            public Builder(Project project, VersionStamp version)
            {
                _project = project;
                _version = version;

                _lazySet = null;
                _lazySyntaxLocals = null;
                _lazySemanticLocals = null;
                _lazyNonLocals = null;
                _lazyOthers = null;
            }

            public AnalysisResult ToResult()
            {
                var documentIds = _lazySet == null ? ImmutableHashSet<DocumentId>.Empty : _lazySet.ToImmutableHashSet();
                var syntaxLocals = Convert(_lazySyntaxLocals);
                var semanticLocals = Convert(_lazySemanticLocals);
                var nonLocals = Convert(_lazyNonLocals);
                var others = _lazyOthers == null ? ImmutableArray<DiagnosticData>.Empty : _lazyOthers.ToImmutableArray();

                return new AnalysisResult(_project.Id, _version, syntaxLocals, semanticLocals, nonLocals, others, documentIds);
            }

            private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> Convert(Dictionary<DocumentId, List<DiagnosticData>> map)
            {
                return map == null ? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty : map.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableArray());
            }

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
                Contract.ThrowIfTrue(_project.SupportsCompilation);

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
                                    var document = _project.GetDocument(diagnosticDocumentId);
                                    if (document != null)
                                    {
                                        // local diagnostics to a file
                                        lazyLocals = lazyLocals ?? new Dictionary<DocumentId, List<DiagnosticData>>();
                                        lazyLocals.GetOrAdd(document.Id, _ => new List<DiagnosticData>()).Add(DiagnosticData.Create(document, diagnostic));

                                        SetDocument(document);
                                    }
                                }
                                else if (diagnosticDocumentId != null)
                                {
                                    var document = _project.GetDocument(diagnosticDocumentId);
                                    if (document != null)
                                    {
                                        // non local diagnostics to a file
                                        _lazyNonLocals = _lazyNonLocals ?? new Dictionary<DocumentId, List<DiagnosticData>>();
                                        _lazyNonLocals.GetOrAdd(document.Id, _ => new List<DiagnosticData>()).Add(DiagnosticData.Create(document, diagnostic));

                                        SetDocument(document);
                                    }
                                }
                                else
                                {
                                    // non local diagnostics without location
                                    _lazyOthers = _lazyOthers ?? new List<DiagnosticData>();
                                    _lazyOthers.Add(DiagnosticData.Create(_project, diagnostic));
                                }

                                break;
                            }
                        case LocationKind.None:
                            {
                                _lazyOthers = _lazyOthers ?? new List<DiagnosticData>();
                                _lazyOthers.Add(DiagnosticData.Create(_project, diagnostic));
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
                Contract.Requires(dummy == null);
            }

            private void AddDiagnostics(
                ref Dictionary<DocumentId, List<DiagnosticData>> lazyLocals, SyntaxTree tree, IEnumerable<Diagnostic> diagnostics)
            {
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
                                _lazyOthers = _lazyOthers ?? new List<DiagnosticData>();
                                _lazyOthers.Add(DiagnosticData.Create(_project, diagnostic));
                                break;
                            }
                        case LocationKind.SourceFile:
                            {
                                if (tree != null && diagnostic.Location.SourceTree == tree)
                                {
                                    var document = GetDocument(diagnostic);
                                    if (document != null)
                                    {
                                        // local diagnostics to a file
                                        lazyLocals = lazyLocals ?? new Dictionary<DocumentId, List<DiagnosticData>>();
                                        lazyLocals.GetOrAdd(document.Id, _ => new List<DiagnosticData>()).Add(DiagnosticData.Create(document, diagnostic));

                                        SetDocument(document);
                                    }
                                }
                                else if (diagnostic.Location.SourceTree != null)
                                {
                                    var document = _project.GetDocument(diagnostic.Location.SourceTree);
                                    if (document != null)
                                    {
                                        // non local diagnostics to a file
                                        _lazyNonLocals = _lazyNonLocals ?? new Dictionary<DocumentId, List<DiagnosticData>>();
                                        _lazyNonLocals.GetOrAdd(document.Id, _ => new List<DiagnosticData>()).Add(DiagnosticData.Create(document, diagnostic));

                                        SetDocument(document);
                                    }
                                }
                                else
                                {
                                    // non local diagnostics without location
                                    _lazyOthers = _lazyOthers ?? new List<DiagnosticData>();
                                    _lazyOthers.Add(DiagnosticData.Create(_project, diagnostic));
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

            private void SetDocument(Document document)
            {
                _lazySet = _lazySet ?? new HashSet<DocumentId>();
                _lazySet.Add(document.Id);
            }

            private Document GetDocument(Diagnostic diagnostic)
            {
                return _project.GetDocument(diagnostic.Location.SourceTree);
            }

            private DocumentId GetExternalDocumentId(Diagnostic diagnostic)
            {
                var projectId = _project.Id;
                var lineSpan = diagnostic.Location.GetLineSpan();

                return _project.Solution.GetDocumentIdsWithFilePath(lineSpan.Path).FirstOrDefault(id => id.ProjectId == projectId);
            }
        }
    }
}
