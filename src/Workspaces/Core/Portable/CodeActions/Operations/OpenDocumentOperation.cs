﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            _documentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
            _activate = activateIfAlreadyOpen;
        }

        public DocumentId DocumentId => _documentId;

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.CanOpenDocuments)
            {
                workspace.OpenDocument(_documentId, _activate);
            }
        }
    }
}
