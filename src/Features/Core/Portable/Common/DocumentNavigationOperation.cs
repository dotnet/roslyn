// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

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
        internal DocumentId DocumentId { get; }
        internal int Position { get; }

        public DocumentNavigationOperation(DocumentId documentId, int position = 0)
        {
            DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
            Position = position;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            // Intentionally empty.  Handling of this operation is special cased in CodeActionEditHandlerService.cs 
        }
    }
}
