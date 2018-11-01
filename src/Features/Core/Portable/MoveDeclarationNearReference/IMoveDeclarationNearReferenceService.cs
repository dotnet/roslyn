// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MoveDeclarationNearReference
{
    internal interface IMoveDeclarationNearReferenceService : ILanguageService
    {
        /// <summary>
        /// Returns true if <paramref name="localDeclarationStatement"/> is local declaration statement
        /// that can be moved forward to be closer to its first reference.
        /// </summary>
        Task<bool> CanMoveDeclarationNearReferenceAsync(Document document, SyntaxNode localDeclarationStatement, CancellationToken cancellationToken);

        /// <summary>
        /// Moves <paramref name="localDeclarationStatement"/> closer to its first reference. Only
        /// applicable if <see cref="CanMoveDeclarationNearReferenceAsync"/> returned
        /// <code>true</code>.  If not, then the original document will be returned unchanged.
        /// </summary>
        Task<Document> MoveDeclarationNearReferenceAsync(Document document, SyntaxNode localDeclarationStatement, CancellationToken cancellationToken);
    }
}
