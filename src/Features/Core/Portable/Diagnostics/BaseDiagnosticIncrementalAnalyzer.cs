// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class BaseDiagnosticIncrementalAnalyzer : IIncrementalAnalyzer
    {
        protected BaseDiagnosticIncrementalAnalyzer(DiagnosticAnalyzerService owner, Workspace workspace, HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            this.Owner = owner;
            this.Workspace = workspace;
            this.HostAnalyzerManager = hostAnalyzerManager;
            this.HostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            this.DiagnosticLogAggregator = new DiagnosticLogAggregator(owner);
        }

        #region IIncrementalAnalyzer
        /// <summary>
        /// Analyze a single document such that local diagnostics for that document become available,
        /// prioritizing analyzing this document over analyzing the rest of the project.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> for each
        /// unique group of diagnostics, where a group is identified by analysis classification (syntax/semantics), document, and analyzer.
        /// </summary>
        /// <param name="document">The document to analyze.</param>
        /// <param name="bodyOpt">If present, indicates a portion (e.g. a method body) of the document to analyze.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken);
        /// <summary>
        /// Analyze a single project such that diagnostics for the entire project become available.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> for each
        /// unique group of diagnostics, where a group is identified by analysis classification (project), project, and analyzer.
        /// </summary>
        /// <param name="project">The project to analyze.</param>
        /// <param name="semanticsChanged">Indicates a change to the declarative semantics of the project.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken);
        /// <summary>
        /// Apply syntax tree actions (that have not already been applied) to a document.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> for each
        /// unique group of diagnostics, where a group is identified by analysis classification (syntax), document, and analyzer.
        /// </summary>
        /// <param name="document">The document to analyze.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// Respond to a document being opened for editing in the host.
        /// </summary>
        /// <param name="document">The opened document.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// Respond to a document being closed in the host.
        /// </summary>
        /// <param name="document">The closed document.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task DocumentCloseAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// Flush cached diagnostics produced by a prior analysis of a document. 
        /// This will be called in a case where we need to re-analyze a document without textual, syntactic or semantic change of a solution.
        /// For example, engine or analyzer's option changes.
        /// </summary>
        /// <param name="document">The document whose diagnostics are to be flushed.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task DocumentResetAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// ???
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);
        /// <summary>
        /// Flush diagnostics produced by a prior analysis of a document,
        /// and suppress future analysis of the document.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> with an empty collection.
        /// </summary>
        /// <param name="documentId"></param>
        public abstract void RemoveDocument(DocumentId documentId);
        /// <summary>
        /// Flush diagnostics produced by a prior analysis of a project,
        /// and suppress future analysis of the project.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> with an empty collection.
        /// </summary>
        /// <param name="projectId"></param>
        public abstract void RemoveProject(ProjectId projectId);
        #endregion

        #region delegating methods from diagnostic analyzer service to each implementation of the engine
        /// <summary>
        /// Get previously-computed (and potentially stale) diagnostics associated with a particular combination of
        /// analysis classification (syntax/semantics/project), document/project, and analyzer.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="id">Matched against values supplied in a <see cref="DiagnosticsUpdatedArgs"/> to <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/>.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get previously-computed (and potentially stale) diagnostics associated with a particular document, project, or solution.
        /// </summary>
        /// <param name="solution">The solution. If projectId and documentId are both null, returned diagnostics are for the entire solution.</param>
        /// <param name="projectId">If projectId is non null and documentId is null, returned diagnostics are for that project only.</param>
        /// <param name="documentId">If documentId is non null, returned diagnostics are for that document only.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics associated with a particular combination of
        /// analysis classification (syntax/semantics/project), document/project, and analyzer.
        /// </summary>
        /// <param name="solution">The solution.</param>
        /// <param name="id">Matched against values supplied in a <see cref="DiagnosticsUpdatedArgs"/> to <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/>.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics associated with a particular document, project, or solution.
        /// </summary>
        /// <param name="solution">The solution. If projectId and documentId are both null, returned diagnostics are for the entire solution.</param>
        /// <param name="projectId">If projectId is non null and documentId is null, returned diagnostics are for that project only.</param>
        /// <param name="documentId">If documentId is non null, returned diagnostics are for that document only.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics matching one of a set of diagnostic IDs associated with a particular document, project, or solution.
        /// </summary>
        /// <param name="solution">The solution. If projectId and documentId are both null, returned diagnostics are for the entire solution.</param>
        /// <param name="projectId">If projectId is non null and documentId is null, returned diagnostics are for that project only.</param>
        /// <param name="documentId">If documentId is non null, returned diagnostics are for that document only.</param>
        /// <param name="diagnosticIds">The diagnostic IDs to match.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics matching one of a set of diagnostic IDs that are not associated with a particular document.
        /// </summary>
        /// <param name="solution">The solution. If projectId is null, returned diagnostics are for the entire solution.</param>
        /// <param name="projectId">If projectId is non null, returned diagnostics are for that project only.</param>
        /// <param name="diagnosticIds">The diagnostic IDs to match.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Add diagnostics local to a span to a list of diagnostics.
        /// </summary>
        /// <param name="document">The document containing the span.</param>
        /// <param name="range">The span for which to produce diagnostics.</param>
        /// <param name="diagnostics">The list of diagnostics to be augmented.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if the set of results is complete, false if getting a complete set requires running per-document actions.</returns>
        public abstract Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics local to a span.
        /// </summary>
        /// <param name="document">The document containing the span.</param>
        /// <param name="range">The span for which to produce diagnostics.</param>
        /// <param name="includeSuppressedDiagnostics">Flag indicating whether diagnostics with source suppressions (pragma/SuppressMessageAttribute) should be included.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken));
        #endregion

        #region build error synchronization
        /// <summary>
        /// Callback from build listener.
        /// 
        /// Given diagnostics are errors host got from explicit build.
        /// It is up to each incremental analyzer how they will merge this information with live diagnostic info.
        /// 
        /// this API doesn't have cancellationToken since it can't be cancelled.
        /// 
        /// given diagnostics are project wide diagnostics that doesn't contain a source location.
        /// </summary>
        public abstract Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Project project, ImmutableArray<DiagnosticData> diagnostics);

        /// <summary>
        /// Callback from build listener.
        /// 
        /// Given diagnostics are errors host got from explicit build.
        /// It is up to each incremental analyzer how they will merge this information with live diagnostic info
        /// 
        /// this API doesn't have cancellationToken since it can't be cancelled.
        /// 
        /// given diagnostics are ones that has a source location.
        /// </summary>
        public abstract Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Document document, ImmutableArray<DiagnosticData> diagnostics);
        #endregion

        internal DiagnosticAnalyzerService Owner { get; }
        internal Workspace Workspace { get; }
        internal AbstractHostDiagnosticUpdateSource HostDiagnosticUpdateSource { get; }
        internal HostAnalyzerManager HostAnalyzerManager { get; }
        internal DiagnosticLogAggregator DiagnosticLogAggregator { get; private set; }

        protected void ResetDiagnosticLogAggregator()
        {
            DiagnosticLogAggregator = new DiagnosticLogAggregator(Owner);
        }

        public virtual bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return false;
        }

        public virtual void LogAnalyzerCountSummary()
        {
        }

        // internal for testing purposes.
        internal Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException(ProjectId projectId)
        {
            return Owner?.GetOnAnalyzerException(projectId, DiagnosticLogAggregator);
        }
    }
}
