// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface INewUnitTestingIncrementalAnalyzerImplementation
    {
#if false // Not used in unit testing crawling
        // Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);
        // Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        // Task DocumentCloseAsync(Document document, CancellationToken cancellationToken);
        // Task DocumentResetAsync(Document document, CancellationToken cancellationToken);
        // void RemoveProject(ProjectId projectId);

        // [Obsolete]
        // bool NeedsReanalysisOnOptionChanged(object sender, UnitTestingOptionChangedEventArgsWrapper e);
#endif

        Task AnalyzeSyntaxAsync(Document document, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken);
        Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken);
        Task AnalyzeProjectAsync(Project project, bool semanticsChanged, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken);
        void RemoveDocument(DocumentId documentId);
    }
}
