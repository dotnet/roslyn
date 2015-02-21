// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        #region IIncrementalAnalyzer
        public abstract Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken);
        public abstract Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken);
        public abstract Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken);
        public abstract Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        public abstract Task DocumentResetAsync(Document document, CancellationToken cancellationToken);
        public abstract Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);
        public abstract void RemoveDocument(DocumentId documentId);
        public abstract void RemoveProject(ProjectId projectId);
        #endregion

        #region delegating methods from diagnostic analyzer service to each implementation of the engine
        public abstract Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken));
        public abstract Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, CancellationToken cancellationToken);
        public abstract Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken);
        #endregion

        public virtual bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return false;
        }

        public virtual void LogAnalyzerCountSummary()
        {
        }
    }
}
