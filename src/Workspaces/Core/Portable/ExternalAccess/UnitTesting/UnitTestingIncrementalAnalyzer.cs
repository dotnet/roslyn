// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private static UnitTestingIncrementalAnalyzer s_instance;

        public static UnitTestingIncrementalAnalyzer GetInstance(IUnitTestingIncrementalAnalyzerImplementation implementation)
        {
            if (s_instance == null)
            {
                s_instance = new UnitTestingIncrementalAnalyzer(implementation);
            }

            if (s_instance._implementation != implementation)
            {
                // NOTE: The implementation should be a singleton.
                throw new InvalidOperationException();
            }

            return s_instance;
        }

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

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            => _implementation.NeedsReanalysisOnOptionChanged(sender, new UnitTestingOptionChangedEventArgsWrapper(e));

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            => _implementation.NewSolutionSnapshotAsync(solution, cancellationToken);

        public void RemoveDocument(DocumentId documentId)
            => _implementation.RemoveDocument(documentId);

        public void RemoveProject(ProjectId projectId)
            => _implementation.RemoveProject(projectId);

        // Unit testing incremental analyzer only supports full solution analysis scope.
        // In future, we should add a separate option to allow users to configure background analysis scope for unit testing.
        public BackgroundAnalysisScope GetBackgroundAnalysisScope(OptionSet _) => BackgroundAnalysisScope.FullSolution;
    }
}
