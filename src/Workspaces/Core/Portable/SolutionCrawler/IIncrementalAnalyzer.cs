// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal interface IIncrementalAnalyzer
    {
        Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);

        Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        Task DocumentCloseAsync(Document document, CancellationToken cancellationToken);

        /// <summary>
        /// Resets all the document state cached by the analyzer.
        /// </summary>
        Task DocumentResetAsync(Document document, CancellationToken cancellationToken);

        Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken);
        Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken);
        Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken);

        Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken);
        Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken);

        bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e);

        /// <summary>
        /// Flag indicating if the analysis result for documents are dependent on whether or not it is an active document.
        /// </summary>
        bool IsDocumentAnalysisDependentOnItBeingActiveDocumentOrNot { get; }
    }
}
