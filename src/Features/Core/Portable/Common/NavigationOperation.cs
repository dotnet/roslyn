// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A <see cref="CodeActionOperation"/> for navigating to a specific position in a document.
    /// When <see cref="CodeAction.GetOperationsAsync(CancellationToken)"/> is called an implementation
    /// of <see cref="CodeAction"/> can return an instance of this operation along with the other 
    /// operations they want to apply.  For example, an implementation could generate a new <see cref="Document"/>
    /// in one <see cref="CodeActionOperation"/> and then have the host editor navigate to that
    /// <see cref="Document"/> using this operation.
    /// </summary>
    public class DocumentNavigationOperation : CodeActionOperation
    {
        private readonly DocumentId _documentId;
        private readonly int _position;

        public DocumentNavigationOperation(DocumentId documentId, int position = 0)
        {
            _documentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
            _position = position;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.CanOpenDocuments)
            {
                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
                navigationService.TryNavigateToPosition(workspace, _documentId, _position);
            }
        }
    }
}
