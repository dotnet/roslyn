// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis
{
    internal class DocumentChangeAction : CodeAction
    {
        private readonly string _title;
        private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;
        private readonly string _equivalenceKey;

        public DocumentChangeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey = null)
        {
            _title = title;
            _createChangedDocument = createChangedDocument;
            _equivalenceKey = equivalenceKey;
        }

        public override string Title
        {
            get { return _title; }
        }

        public override string EquivalenceKey
        {
            get { return _equivalenceKey; }
        }

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            return _createChangedDocument(cancellationToken);
        }
    }
}
