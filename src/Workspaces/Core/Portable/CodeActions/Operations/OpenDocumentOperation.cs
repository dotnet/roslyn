// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A code action operation for requesting a document be opened in the host environment.
    /// </summary>
    public sealed class OpenDocumentOperation : CodeActionOperation
    {
        private readonly DocumentId _documentId;
        private readonly bool _activate;

        public OpenDocumentOperation(DocumentId documentId, bool activateIfAlreadyOpen = false)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            _documentId = documentId;
            _activate = activateIfAlreadyOpen;
        }

        public DocumentId DocumentId
        {
            get { return _documentId; }
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.CanOpenDocuments)
            {
                workspace.OpenDocument(_documentId, _activate);
            }
        }
    }
}
