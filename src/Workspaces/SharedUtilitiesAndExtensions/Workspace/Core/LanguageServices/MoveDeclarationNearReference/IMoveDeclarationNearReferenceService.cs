// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
