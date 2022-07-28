// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    /// <summary>
    /// Used for semantic analysis callbacks into <see cref="IIncrementalAnalyzer"/>s
    /// when only a single member node, such as a method body, in the document has been
    /// edited from the prior document snapshot.
    /// </summary>
    /// <param name="ChangedMemberNode">Member node which was edited.</param>
    /// <param name="OldVersion">Semantic version of the old project prior to this member edit.</param>
    /// <param name="NewVersion">Semantic version of the new project after this member edit.</param>
    internal record class ChangedMemberNodeWithVersions(SyntaxNode ChangedMemberNode, VersionStamp OldVersion, VersionStamp NewVersion);

    internal interface IIncrementalAnalyzer
    {
        Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken);

        Task DocumentOpenAsync(Document document, CancellationToken cancellationToken);
        Task DocumentCloseAsync(Document document, CancellationToken cancellationToken);
        Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken);

        /// <summary>
        /// Resets all the document state cached by the analyzer.
        /// </summary>
        Task DocumentResetAsync(Document document, CancellationToken cancellationToken);

        Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken);
        Task AnalyzeDocumentAsync(Document document, ChangedMemberNodeWithVersions changedMemberWithVersions, InvocationReasons reasons, CancellationToken cancellationToken);
        Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken);

        Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken);
        Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken);

        Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken);
        Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken);

        /// <summary>
        /// Resets all the document state cached by the analyzer.
        /// </summary>
        Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken);

        Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken);

        void LogAnalyzerCountSummary();
        int Priority { get; }
        void Shutdown();
    }
}
