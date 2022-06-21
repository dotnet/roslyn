// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class IncrementalAnalyzerBase : IIncrementalAnalyzer
    {
        protected IncrementalAnalyzerBase()
        {
        }

        public virtual Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellation)
            => Task.CompletedTask;

        public virtual Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void LogAnalyzerCountSummary()
        {
        }

        /// <summary>
        /// Order all incremental analyzers below DiagnosticIncrementalAnalyzer
        /// </summary>
        public virtual int Priority => 1;

        public virtual void Shutdown()
        {
        }
    }
}
