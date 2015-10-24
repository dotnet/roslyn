// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class EditSession
    {
        [SuppressMessage("Performance", "RS0008", Justification = "Equality not actually implemented")]
        private struct Analysis
        {
            public readonly Document Document;
            public readonly AsyncLazy<DocumentAnalysisResults> Results;

            public Analysis(Document document, AsyncLazy<DocumentAnalysisResults> results)
            {
                this.Document = document;
                this.Results = results;
            }

            public override bool Equals(object obj)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override int GetHashCode()
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private readonly Solution _baseSolution;

        // signaled when the session is terminated:
        private readonly CancellationTokenSource _cancellation;

        // document id -> [active statements ordered by position]
        private readonly IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatementSpan>> _baseActiveStatements;

        private readonly DebuggingSession _debuggingSession;

        /// <summary>
        /// Stopped at exception, an unwind is required before EnC is allowed. All edits are rude.
        /// </summary>
        private readonly bool _stoppedAtException;

        // Results of changed documents analysis. 
        // The work is triggered by an incremental analyzer on idle or explicitly when "continue" operation is executed.
        // Contains analyses of the latest observed document versions.
        private readonly object _analysesGuard = new object();
        private readonly Dictionary<DocumentId, Analysis> _analyses;

        // A document id is added whenever any analysis reports rude edits.
        // We collect a set of document ids that contained a rude edit
        // at some point in time during the lifespan of an edit session.
        // At the end of the session we aks the diagnostic analyzer to reanalyze 
        // the documents to clean up the diagnostics.
        // An id may be present in this set even if the document doesn't have a rude edit anymore.
        private readonly object _documentsWithReportedRudeEditsGuard = new object();
        private readonly HashSet<DocumentId> _documentsWithReportedRudeEdits;

        private readonly ImmutableDictionary<ProjectId, ProjectReadOnlyReason> _projects;

        // EncEditSessionInfo is populated on a background thread and then read from the UI thread
        private readonly object _encEditSessionInfoGuard = new object();
        private EncEditSessionInfo _encEditSessionInfo = new EncEditSessionInfo();

        internal EditSession(
            Solution baseSolution,
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatementSpan>> baseActiveStatements,
            DebuggingSession debuggingSession,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            bool stoppedAtException)
        {
            Debug.Assert(baseSolution != null);
            Debug.Assert(debuggingSession != null);

            _baseSolution = baseSolution;
            _debuggingSession = debuggingSession;
            _stoppedAtException = stoppedAtException;
            _projects = projects;
            _cancellation = new CancellationTokenSource();

            // TODO: small dict, pool?
            _analyses = new Dictionary<DocumentId, Analysis>();
            _baseActiveStatements = baseActiveStatements;

            // TODO: small dict, pool?
            _documentsWithReportedRudeEdits = new HashSet<DocumentId>();
        }

        internal CancellationTokenSource Cancellation
        {
            get { return _cancellation; }
        }

        internal Solution BaseSolution
        {
            get
            {
                return _baseSolution;
            }
        }

        internal IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatementSpan>> BaseActiveStatements
        {
            get
            {
                return _baseActiveStatements;
            }
        }

        private Solution CurrentSolution
        {
            get
            {
                return _baseSolution.Workspace.CurrentSolution;
            }
        }

        public bool StoppedAtException
        {
            get { return _stoppedAtException; }
        }

        public IReadOnlyDictionary<ProjectId, ProjectReadOnlyReason> Projects
        {
            get { return _projects; }
        }

        internal bool HasProject(ProjectId id)
        {
            ProjectReadOnlyReason reason;
            return Projects.TryGetValue(id, out reason);
        }

        private List<ValueTuple<DocumentId, AsyncLazy<DocumentAnalysisResults>>> GetChangedDocumentsAnalyses(Project baseProject, Project project)
        {
            var changes = project.GetChanges(baseProject);
            var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());
            var result = new List<ValueTuple<DocumentId, AsyncLazy<DocumentAnalysisResults>>>();

            lock (_analysesGuard)
            {
                foreach (var changedDocumentId in changedDocuments)
                {
                    result.Add(ValueTuple.Create(changedDocumentId, GetDocumentAnalysisNoLock(project.GetDocument(changedDocumentId))));
                }
            }

            return result;
        }

        private async Task<HashSet<ISymbol>> GetAllAddedSymbols(CancellationToken cancellationToken)
        {
            Analysis[] analyses;
            lock (_analysesGuard)
            {
                analyses = _analyses.Values.ToArray();
            }

            HashSet<ISymbol> addedSymbols = null;
            foreach (var analysis in analyses)
            {
                var results = await analysis.Results.GetValueAsync(cancellationToken).ConfigureAwait(false);
                foreach (var edit in results.SemanticEdits)
                {
                    if (edit.Kind == SemanticEditKind.Insert)
                    {
                        if (addedSymbols == null)
                        {
                            addedSymbols = new HashSet<ISymbol>();
                        }

                        addedSymbols.Add(edit.NewSymbol);
                    }
                }
            }

            return addedSymbols;
        }

        public AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysis(Document document)
        {
            lock (_analysesGuard)
            {
                return GetDocumentAnalysisNoLock(document);
            }
        }

        private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Document document)
        {
            Analysis analysis;
            if (_analyses.TryGetValue(document.Id, out analysis) && analysis.Document == document)
            {
                return analysis.Results;
            }

            var analyzer = document.Project.LanguageServices.GetService<IEditAndContinueAnalyzer>();

            ImmutableArray<ActiveStatementSpan> activeStatements;
            if (!_baseActiveStatements.TryGetValue(document.Id, out activeStatements))
            {
                activeStatements = ImmutableArray.Create<ActiveStatementSpan>();
            }

            var lazyResults = new AsyncLazy<DocumentAnalysisResults>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    try
                    {
                        var result = await analyzer.AnalyzeDocumentAsync(_baseSolution, activeStatements, document, cancellationToken).ConfigureAwait(false);

                        if (!result.RudeEditErrors.IsDefault)
                        {
                            lock (_documentsWithReportedRudeEditsGuard)
                            {
                                _documentsWithReportedRudeEdits.Add(document.Id);
                            }
                        }

                        return result;
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                },
                cacheResult: true);

            _analyses[document.Id] = new Analysis(document, lazyResults);
            return lazyResults;
        }

        internal ImmutableArray<DocumentId> GetDocumentsWithReportedRudeEdits()
        {
            lock (_documentsWithReportedRudeEditsGuard)
            {
                return ImmutableArray.CreateRange(_documentsWithReportedRudeEdits);
            }
        }

        public async Task<ProjectAnalysisSummary> GetProjectAnalysisSummaryAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                var baseProject = _baseSolution.GetProject(project.Id);

                // TODO (https://github.com/dotnet/roslyn/issues/1204):
                if (baseProject == null)
                {
                    return ProjectAnalysisSummary.NoChanges;
                }

                var documentAnalyses = GetChangedDocumentsAnalyses(baseProject, project);
                if (documentAnalyses.Count == 0)
                {
                    return ProjectAnalysisSummary.NoChanges;
                }

                bool hasChanges = false;
                bool hasSignificantChanges = false;

                foreach (var analysis in documentAnalyses)
                {
                    var result = await analysis.Item2.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // skip documents that actually were not changed:
                    if (!result.HasChanges)
                    {
                        continue;
                    }

                    // rude edit detection wasn't completed due to errors in compilation:
                    if (result.HasChangesAndCompilationErrors)
                    {
                        return ProjectAnalysisSummary.CompilationErrors;
                    }

                    // rude edits detected:
                    if (result.RudeEditErrors.Length != 0)
                    {
                        return ProjectAnalysisSummary.RudeEdits;
                    }

                    hasChanges = true;
                    hasSignificantChanges |= result.HasSignificantChanges;
                }

                if (!hasChanges)
                {
                    // we get here if a document is closed and reopen without any actual change:
                    return ProjectAnalysisSummary.NoChanges;
                }

                if (_stoppedAtException)
                {
                    // all edits are disallowed when stopped at exception:
                    return ProjectAnalysisSummary.RudeEdits;
                }

                return hasSignificantChanges ?
                    ProjectAnalysisSummary.ValidChanges :
                    ProjectAnalysisSummary.ValidInsignificantChanges;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<ProjectChanges> GetProjectChangesAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                var baseProject = _baseSolution.GetProject(project.Id);
                var allEdits = new List<SemanticEdit>();
                var allLineEdits = new List<KeyValuePair<DocumentId, ImmutableArray<LineChange>>>();

                foreach (var analysis in GetChangedDocumentsAnalyses(baseProject, project))
                {
                    var documentId = analysis.Item1;
                    var result = await analysis.Item2.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // we shouldn't be asking for deltas in presence of errors:
                    Debug.Assert(!result.HasChangesAndErrors);

                    allEdits.AddRange(result.SemanticEdits);
                    if (result.LineEdits.Length > 0)
                    {
                        allLineEdits.Add(KeyValuePair.Create(documentId, result.LineEdits));
                    }
                }

                // Ideally we shouldn't be asking for deltas in absence of significant changes.
                // But in VS we have no way of telling the debugger that the changes made 
                // to the source are not significant. So we emit an empty delta.
                // Debug.Assert(allEdits.Count > 0 || allLineEdits.Count > 0);

                return new ProjectChanges(allEdits, allLineEdits);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async Task<Deltas> EmitProjectDeltaAsync(Project project, EmitBaseline baseline, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(!_stoppedAtException);

                var changes = await GetProjectChangesAsync(project, cancellationToken).ConfigureAwait(false);
                var currentCompilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var allAddedSymbols = await GetAllAddedSymbols(cancellationToken).ConfigureAwait(false);

                var pdbStream = new MemoryStream();
                var updatedMethods = new List<MethodDefinitionHandle>();

                using (var metadataStream = SerializableBytes.CreateWritableStream())
                using (var ilStream = SerializableBytes.CreateWritableStream())
                {
                    EmitDifferenceResult result = currentCompilation.EmitDifference(
                        baseline,
                        changes.SemanticEdits,
                        s => allAddedSymbols?.Contains(s) ?? false,
                        metadataStream,
                        ilStream,
                        pdbStream,
                        updatedMethods,
                        cancellationToken);

                    int[] updateMethodTokens = updatedMethods.Select(h => MetadataTokens.GetToken(h)).ToArray();
                    return new Deltas(ilStream.ToArray(), metadataStream.ToArray(), updateMethodTokens, pdbStream, changes.LineChanges, result);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal void LogRudeEditErrors(ImmutableArray<RudeEditDiagnostic> rudeEditErrors)
        {
            lock (_encEditSessionInfoGuard)
            {
                if (_encEditSessionInfo != null)
                {
                    foreach (var item in rudeEditErrors)
                    {
                        _encEditSessionInfo.LogRudeEdit((ushort)item.Kind, item.SyntaxKind);
                    }
                }
            }
        }

        internal void LogEmitProjectDeltaErrors(IEnumerable<string> errorIds)
        {
            lock (_encEditSessionInfoGuard)
            {
                Debug.Assert(_encEditSessionInfo != null);
                _encEditSessionInfo.EmitDeltaErrorIds = errorIds;
            }
        }

        internal void LogBuildState(ProjectAnalysisSummary lastEditSessionSummary)
        {
            lock (_encEditSessionInfoGuard)
            {
                Debug.Assert(_encEditSessionInfo != null);
                _encEditSessionInfo.HadCompilationErrors |= lastEditSessionSummary == ProjectAnalysisSummary.CompilationErrors;
                _encEditSessionInfo.HadRudeEdits |= lastEditSessionSummary == ProjectAnalysisSummary.RudeEdits;
                _encEditSessionInfo.HadValidChanges |= lastEditSessionSummary == ProjectAnalysisSummary.ValidChanges;
                _encEditSessionInfo.HadValidInsignificantChanges |= lastEditSessionSummary == ProjectAnalysisSummary.ValidInsignificantChanges;
            }
        }

        internal void LogEditSession(EncDebuggingSessionInfo encDebuggingSessionInfo)
        {
            lock (_encEditSessionInfoGuard)
            {
                Debug.Assert(_encEditSessionInfo != null);
                encDebuggingSessionInfo.EndEditSession(_encEditSessionInfo);
                _encEditSessionInfo = null;
            }
        }
    }
}
