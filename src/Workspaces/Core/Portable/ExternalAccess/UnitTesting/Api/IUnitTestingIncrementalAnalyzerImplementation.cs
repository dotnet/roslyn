// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface IUnitTestingIncrementalAnalyzerImplementation
    {
        Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);
        Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        Task DocumentCloseAsync(Document document, CancellationToken cancellationToken);
        Task DocumentResetAsync(Document document, CancellationToken cancellationToken);
        Task AnalyzeSyntaxAsync(Document document, UnitTestingInvocationReasonsWrapper reasons, CancellationToken cancellationToken);
        Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, UnitTestingInvocationReasonsWrapper reasons, CancellationToken cancellationToken);
        Task AnalyzeProjectAsync(Project project, bool semanticsChanged, UnitTestingInvocationReasonsWrapper reasons, CancellationToken cancellationToken);
        void RemoveDocument(DocumentId documentId);
        void RemoveProject(ProjectId projectId);
        bool NeedsReanalysisOnOptionChanged(object sender, UnitTestingOptionChangedEventArgsWrapper e);
    }
}
