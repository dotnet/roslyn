// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.CodeActions
{
#pragma warning disable RS0030 // Do not used banned APIs
    /// <summary>
    /// A <see cref="CodeActionOperation"/> for navigating to a specific position in a document and invoking inline rename.
    /// When <see cref="CodeAction.GetOperationsAsync(System.Threading.CancellationToken)"/> is called an implementation
    /// of <see cref="CodeAction"/> can return an instance of this operation along with the other 
    /// operations they want to apply. For example, an implementation could generate a new <see cref="Document"/>
    /// in one <see cref="CodeActionOperation"/> and then have the host editor navigate to that
    /// <see cref="Document"/> and invoke rename at a given position using this operation.
    /// </summary>
#pragma warning restore RS0030 // Do not used banned APIs
    internal sealed class StartInlineRenameSessionOperation(DocumentId documentId, int position) : CodeActionOperation
    {
        public DocumentId DocumentId { get; } = documentId ?? throw new ArgumentNullException(nameof(documentId));
        public int Position { get; } = position;

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            // Intentionally empty.  Handling of this operation is special cased in CodeActionEditHandlerService.cs 
        }
    }
}
