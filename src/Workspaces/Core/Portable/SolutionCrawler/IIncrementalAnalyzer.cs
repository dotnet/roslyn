// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken);

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken);

        void RemoveDocument(DocumentId documentId);
        void RemoveProject(ProjectId projectId);

        bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e);
    }
}
