// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class BaseDiagnosticIncrementalAnalyzer : IIncrementalAnalyzer
    {
        protected BaseDiagnosticIncrementalAnalyzer(Workspace workspace)
        {
            this.Workspace = workspace;
        }

        #region IIncrementalAnalyzer
        /// <summary>
        /// Analyze a single document such that local diagnostics for that document become available,
        /// prioritizing analyzing this document over analyzing the rest of the project.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> for each
        /// unique group of diagnostics, where a group is identified by analysis classification (syntax/semantics), document, and analyzer.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="bodyOpt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken);
        /// <summary>
        /// Analyze a single project such that diagnostics for the entire project become available.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> for each
        /// unique group of diagnostics, where a group is identified by analysis classification (project), project, and analyzer.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="semanticsChanged"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken);
        /// <summary>
        /// Apply syntax tree actions (that have not already been applied) to a document.
        /// Calls <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/> for each
        /// unique group of diagnostics, where a group is identified by analysis classification (syntax), document, and analyzer.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// Respond to a document being opened for editing in the host.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// Flush cached diagnostics produced by a prior analysis of a document.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task DocumentResetAsync(Document document, CancellationToken cancellationToken);
        /// <summary>
        /// This method appears not to need to have an effect.
        /// </summary>
        /// <param name="solution"></param>
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
        /// <param name="solution"></param>
        /// <param name="id">Matched against values supplied in a <see cref="DiagnosticsUpdatedArgs"/> to <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken);
        /// <summary>
        /// Get previously-computed (and potentially stale) diagnostics associated with a particular document, project, or solution.
        /// </summary>
        /// <param name="solution">Ignored if projectId or documentId is non null.</param>
        /// <param name="projectId">Ignored if documentId is non null.</param>
        /// <param name="documentId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics associated with a particular combination of
        /// analysis classification (syntax/semantics/project), document/project, and analyzer.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="id">Matched against values supplied in a <see cref="DiagnosticsUpdatedArgs"/> to <see cref="DiagnosticAnalyzerService.RaiseDiagnosticsUpdated(object, DiagnosticsUpdatedArgs)"/>.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken);
        /// <summary>
        /// Get diagnostics associated with a particular document, project, or solution.
        /// </summary>
        /// <param name="solution">Ignored if projectId or documentId is non null.</param>
        /// <param name="projectId">Ignored if documentId is non null.</param>
        /// <param name="documentId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics matching one of a set of diagnostic IDs associated with a particular document, project, or solution.
        /// </summary>
        /// <param name="solution">Ignored if projectId or documentId is non null.</param>
        /// <param name="projectId">Ignored if documentId is non null.</param>
        /// <param name="documentId"></param>
        /// <param name="diagnosticIds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Get diagnostics matching one of a set of diagnostic IDs that are not associated with a particular document.
        /// </summary>
        /// <param name="solution">Ignored if projectId is non null.</param>
        /// <param name="projectId"></param>
        /// <param name="diagnosticIds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// Add diagnostics local to a span to a list of diagnostics.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="range"></param>
        /// <param name="diagnostics"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if the set of results is complete, false if getting a complete set requires running per-document actions.</returns>
        public abstract Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, CancellationToken cancellationToken);
        /// <summary>
        /// Get diagnostics local to a span.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="range"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken);
        #endregion

        public Workspace Workspace { get; private set; }

        public virtual bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return false;
        }

        public virtual void LogAnalyzerCountSummary()
        {
        }
    }
}
