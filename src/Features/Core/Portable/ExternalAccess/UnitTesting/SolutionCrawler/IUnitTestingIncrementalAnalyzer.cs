// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal interface IUnitTestingIncrementalAnalyzer
    {
#if false // Not used in unit testing crawling
        Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);

        Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        Task DocumentCloseAsync(Document document, CancellationToken cancellationToken);

        Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken);

        /// <summary>
        /// Resets all the document state cached by the analyzer.
        /// </summary>
        Task DocumentResetAsync(Document document, CancellationToken cancellationToken);

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

        Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken);

#if false // Not used in unit testing crawling
        Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken);

        Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken);
        Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken);

        /// <summary>
        /// Resets all the document state cached by the analyzer.
        /// </summary>
        Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken);

        Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, UnitTestingInvocationReasons reasons, CancellationToken cancellationToken);

        void LogAnalyzerCountSummary();
        int Priority { get; }
        void Shutdown();
#endif
    }
}
