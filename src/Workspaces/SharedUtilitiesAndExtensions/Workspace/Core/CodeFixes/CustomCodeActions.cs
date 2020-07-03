// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal static class CustomCodeActions
    {
        internal abstract class SimpleCodeAction : CodeAction
        {
            public SimpleCodeAction(string title, string equivalenceKey)
            {
                Title = title;
                EquivalenceKey = equivalenceKey;
            }

            public sealed override string Title { get; }
            public sealed override string EquivalenceKey { get; }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                => Task.FromResult<Document>(null);
        }

        internal class DocumentChangeAction : SimpleCodeAction
        {
            private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

            public DocumentChangeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey = null)
                : base(title, equivalenceKey)
            {
                _createChangedDocument = createChangedDocument;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
                => _createChangedDocument(cancellationToken);
        }

        internal class SolutionChangeAction : SimpleCodeAction
        {
            private readonly Func<CancellationToken, Task<Solution>> _createChangedSolution;

            public SolutionChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
                : base(title, equivalenceKey)
            {
                _createChangedSolution = createChangedSolution;
            }

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
                => _createChangedSolution(cancellationToken);
        }
    }
}
