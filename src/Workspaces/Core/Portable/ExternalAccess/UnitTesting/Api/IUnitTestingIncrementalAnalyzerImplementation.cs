// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
    }
}
