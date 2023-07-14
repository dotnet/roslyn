// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CustomCodeActions
    {
        internal abstract class SimpleCodeAction(
            string title,
            string? equivalenceKey) : CodeAction
        {
            public sealed override string Title { get; } = title;
            public sealed override string? EquivalenceKey { get; } = equivalenceKey;
        }

        internal class DocumentChangeAction(
            string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string? equivalenceKey,
            CodeActionPriority priority) : SimpleCodeAction(title, equivalenceKey)
        {
            private readonly Func<CancellationToken, Task<Document>> _createChangedDocument = createChangedDocument;
            private readonly CodeActionPriority _priority = priority;

            protected sealed override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                => _createChangedDocument(cancellationToken);

            protected sealed override CodeActionPriority ComputePriority()
                => _priority;
        }

        internal class SolutionChangeAction(
            string title,
            Func<CancellationToken, Task<Solution>> createChangedSolution,
            string? equivalenceKey) : SimpleCodeAction(title, equivalenceKey)
        {
            protected sealed override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
                => createChangedSolution(cancellationToken).AsNullable();
        }
    }
}
