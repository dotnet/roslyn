// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal class UnitTestingIncrementalAnalyzerBase : IUnitTestingIncrementalAnalyzer
    {
        protected UnitTestingIncrementalAnalyzerBase()
        {
        }

#if false // Not used in unit testing crawling

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

        public virtual Task AnalyzeSyntaxAsync(Document document, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

#endif

        public virtual Task AnalyzeDocumentAsync(
            Document document,
#if false // Not used in unit testing crawling
            SyntaxNode bodyOpt,
#endif
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task AnalyzeProjectAsync(
            Project project,
#if false // Not used in unit testing crawling
            bool semanticsChanged,
#endif
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
            => Task.CompletedTask;

#if false // Not used in unit testing crawling

        public virtual Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellation)
            => Task.CompletedTask;

        public virtual Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
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

#endif
    }
}
