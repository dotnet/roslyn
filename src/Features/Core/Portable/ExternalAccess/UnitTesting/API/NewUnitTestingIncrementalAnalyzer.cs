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
    private class NewUnitTestingIncrementalAnalyzer : IUnitTestingIncrementalAnalyzer
    {
        private readonly INewUnitTestingIncrementalAnalyzerImplementation _implementation;

        public NewUnitTestingIncrementalAnalyzer(INewUnitTestingIncrementalAnalyzerImplementation implementation)
            => _implementation = implementation;

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeDocumentAsync(document, bodyOpt, reasons, cancellationToken);

        public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeProjectAsync(project, semanticsChanged, reasons, cancellationToken);

        public Task AnalyzeSyntaxAsync(Document document, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeSyntaxAsync(document, reasons, cancellationToken);

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

        public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            _implementation.RemoveDocument(documentId);
            return Task.CompletedTask;
        }

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

#if false

    // Unit testing incremental analyzer only supports full solution analysis scope.
    // In future, we should add a separate option to allow users to configure background analysis scope for unit testing.
    public static BackgroundAnalysisScope GetBackgroundAnalysisScope(OptionSet _) => BackgroundAnalysisScope.FullSolution;

#endif

        public void Shutdown()
        {
        }
    }
}
