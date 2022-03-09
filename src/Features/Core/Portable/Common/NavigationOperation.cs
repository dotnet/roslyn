// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Utilities;

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
                var threadingService = workspace.Services.GetService<IWorkspaceThreadingServiceProvider>();

                threadingService.Service.Run(
                    () => navigationService.TryNavigateToPositionAsync(workspace, _documentId, _position, cancellationToken));
            }
        }
    }
}
