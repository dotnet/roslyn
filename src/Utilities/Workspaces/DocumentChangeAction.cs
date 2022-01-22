// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace Analyzer.Utilities
{
    internal abstract class DocumentChangeAction : CodeAction
    {
        private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

        protected DocumentChangeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
        {
            Title = title;
            _createChangedDocument = createChangedDocument;

            // Null equivalence key causes RS1011
            // A CodeFixProvider that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer.
            // This enables the FixAllProvider to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.
            EquivalenceKey = equivalenceKey ?? throw new ArgumentNullException(equivalenceKey);
        }

        public override string Title { get; }

        public override string EquivalenceKey { get; }

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            return _createChangedDocument(cancellationToken);
        }
    }
}