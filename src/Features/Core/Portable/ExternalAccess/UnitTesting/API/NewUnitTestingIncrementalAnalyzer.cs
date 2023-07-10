// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal sealed partial class NewUnitTestingIncrementalAnalyzerProvider
{
    private sealed class NewUnitTestingIncrementalAnalyzer(INewUnitTestingIncrementalAnalyzerImplementation implementation) : IUnitTestingIncrementalAnalyzer
    {
        private readonly INewUnitTestingIncrementalAnalyzerImplementation _implementation = implementation;

        public Task AnalyzeDocumentAsync(
            Document document,
#if false // Not used in unit testing crawling
            SyntaxNode bodyOpt,
#endif
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            return _implementation.AnalyzeDocumentAsync(
                document,
#if false // Not used in unit testing crawling
                bodyOpt,
#endif
                reasons,
                cancellationToken);
        }

        public Task AnalyzeProjectAsync(
            Project project,
#if false // Not used in unit testing crawling
            bool semanticsChanged,
#endif
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            return _implementation.AnalyzeProjectAsync(
                project,
#if false // Not used in unit testing crawling
                semanticsChanged,
#endif
                reasons,
                cancellationToken);
        }

#if false // Not used in unit testing crawling
        public Task AnalyzeSyntaxAsync(Document document, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            => Task.CompletedTask;
#endif

        public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            _implementation.RemoveDocument(documentId);
            return Task.CompletedTask;
        }

#if false // Not used in unit testing crawling
        public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void LogAnalyzerCountSummary()
        {
        }

        /// <summary>
        /// Order all incremental analyzers below DiagnosticIncrementalAnalyzer
        /// </summary>
        public int Priority => 1;

        // Unit testing incremental analyzer only supports full solution analysis scope.
        // In future, we should add a separate option to allow users to configure background analysis scope for unit testing.
        public static BackgroundAnalysisScope GetBackgroundAnalysisScope(OptionSet _) => BackgroundAnalysisScope.FullSolution;

        public void Shutdown()
        {
        }
#endif
    }
}
