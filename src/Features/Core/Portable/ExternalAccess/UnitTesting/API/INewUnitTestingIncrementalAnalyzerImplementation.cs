// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        Task AnalyzeSyntaxAsync(Document document, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken);
#endif

        Task AnalyzeDocumentAsync(
            Document document,
#if false // Not used in unit testing crawling
            SyntaxNode bodyOpt,
#endif
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken);

        Task AnalyzeProjectAsync(
            Project project,
#if false // Not used in unit testing crawling
            bool semanticsChanged,
#endif
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken);

        void RemoveDocument(DocumentId documentId);
    }
}
