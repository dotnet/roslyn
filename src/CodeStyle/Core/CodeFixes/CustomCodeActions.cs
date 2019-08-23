// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                return Task.FromResult<Document>(null);
            }
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
            {
                return _createChangedDocument(cancellationToken);
            }
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
            {
                return _createChangedSolution(cancellationToken);
            }
        }
    }
}
