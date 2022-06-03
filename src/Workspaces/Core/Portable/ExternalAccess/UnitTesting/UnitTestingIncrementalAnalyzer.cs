// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private readonly IUnitTestingIncrementalAnalyzerImplementation _implementation;

        public UnitTestingIncrementalAnalyzer(IUnitTestingIncrementalAnalyzerImplementation implementation)
            => _implementation = implementation;

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeDocumentAsync(document, bodyOpt, new UnitTestingInvocationReasonsWrapper(reasons), cancellationToken);

        public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeProjectAsync(project, semanticsChanged, new UnitTestingInvocationReasonsWrapper(reasons), cancellationToken);

        public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeSyntaxAsync(document, new UnitTestingInvocationReasonsWrapper(reasons), cancellationToken);

        public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            => _implementation.DocumentCloseAsync(document, cancellationToken);

        public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            => _implementation.DocumentOpenAsync(document, cancellationToken);

        public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            => _implementation.DocumentResetAsync(document, cancellationToken);

        public Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            => _implementation.NeedsReanalysisOnOptionChanged(sender, new UnitTestingOptionChangedEventArgsWrapper(e));

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            => _implementation.NewSolutionSnapshotAsync(solution, cancellationToken);

        public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            _implementation.RemoveDocument(documentId);
            return Task.CompletedTask;
        }

        public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            _implementation.RemoveProject(projectId);
            return Task.CompletedTask;
        }

        public Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
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
    }
}
