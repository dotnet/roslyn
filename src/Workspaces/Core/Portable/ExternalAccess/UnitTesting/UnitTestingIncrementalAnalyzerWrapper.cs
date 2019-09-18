// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingIncrementalAnalyzerWrapper : IIncrementalAnalyzer
    {
        private readonly IUnitTestingIncrementalAnalyzer _implementation;

        public UnitTestingIncrementalAnalyzerWrapper(IUnitTestingIncrementalAnalyzer implementation)
            => _implementation = implementation;

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeDocumentAsync(document, bodyOpt, new UnitTestingInvovationReasonsWrapper(reasons), cancellationToken);

        public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeProjectAsync(project, semanticsChanged, new UnitTestingInvovationReasonsWrapper(reasons), cancellationToken);

        public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            => _implementation.AnalyzeSyntaxAsync(document, new UnitTestingInvovationReasonsWrapper(reasons), cancellationToken);

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
    }
}
