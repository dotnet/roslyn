// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal sealed class UnitTestingIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly Workspace _workspace;
        private readonly IUnitTestingIncrementalAnalyzerProviderImplementation? _incrementalAnalyzerProvider;
        private readonly NewUnitTestingIncrementalAnalyzerProvider? _newProvider;

        private IIncrementalAnalyzer? _lazyAnalyzer;

        internal UnitTestingIncrementalAnalyzerProvider(Workspace workspace, IUnitTestingIncrementalAnalyzerProviderImplementation incrementalAnalyzerProvider)
        {
            _workspace = workspace;
            _incrementalAnalyzerProvider = incrementalAnalyzerProvider;
        }

        internal UnitTestingIncrementalAnalyzerProvider(Workspace workspace, NewUnitTestingIncrementalAnalyzerProvider newProvider)
        {
            _workspace = workspace;
            _newProvider = newProvider;
        }

        // NOTE: We're currently expecting the analyzer to be singleton, so that
        //       analyzers returned when calling this method twice would pass a reference equality check.
        //       One instance should be created by SolutionCrawler, another one by us, when calling the
        //       UnitTestingSolutionCrawlerServiceAccessor.Reanalyze method.
        IIncrementalAnalyzer IIncrementalAnalyzerProvider.CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (_lazyAnalyzer is null)
            {
                if (_newProvider != null)
                {
                    var newProviderAnalyzer = _newProvider.CreateIncrementalAnalyzer();
                    _lazyAnalyzer = new AnalyzerWrapper(newProviderAnalyzer);
                }
                else
                {
                    _lazyAnalyzer = new UnitTestingIncrementalAnalyzer(_incrementalAnalyzerProvider!.CreateIncrementalAnalyzer());
                }
            }

            return _lazyAnalyzer;
        }

        public void Reanalyze()
        {
            if (_newProvider != null)
            {
                _newProvider.Reanalyze();
            }
            else
            {
                var solutionCrawlerService = _workspace.Services.GetService<ISolutionCrawlerService>();
                if (solutionCrawlerService != null)
                {
                    var analyzer = ((IIncrementalAnalyzerProvider)this).CreateIncrementalAnalyzer(_workspace)!;
                    solutionCrawlerService.Reanalyze(_workspace, analyzer, projectIds: null, documentIds: null, highPriority: false);
                }
            }
        }

        public static UnitTestingIncrementalAnalyzerProvider? TryRegister(Workspace workspace, string analyzerName, IUnitTestingIncrementalAnalyzerProviderImplementation provider)
        {
            var solutionCrawlerRegistrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            if (solutionCrawlerRegistrationService == null)
            {
                return null;
            }

            var analyzerProvider = new UnitTestingIncrementalAnalyzerProvider(workspace, provider);

            var metadata = new IncrementalAnalyzerProviderMetadata(
                analyzerName,
                highPriorityForActiveFile: false,
                new[] { workspace.Kind });

            solutionCrawlerRegistrationService.AddAnalyzerProvider(analyzerProvider, metadata);
            return analyzerProvider;
        }

        private class AnalyzerWrapper : IIncrementalAnalyzer
        {
            private readonly IUnitTestingIncrementalAnalyzer _newAnalyzer;

            public AnalyzerWrapper(IUnitTestingIncrementalAnalyzer newAnalyzer)
            {
                _newAnalyzer = newAnalyzer;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
                => _newAnalyzer.AnalyzeDocumentAsync(document, bodyOpt, new UnitTestingInvocationReasons(reasons.Reasons), cancellationToken);

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
                => _newAnalyzer.AnalyzeProjectAsync(project, semanticsChanged, new UnitTestingInvocationReasons(reasons.Reasons), cancellationToken);

            public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
                => _newAnalyzer.RemoveDocumentAsync(documentId, cancellationToken);

            public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
                => Task.CompletedTask;

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

            public int Priority => 1;

            public void Shutdown()
            {
            }
        }
    }
}
