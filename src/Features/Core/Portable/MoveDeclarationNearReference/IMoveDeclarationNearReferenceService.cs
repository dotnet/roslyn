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
        /// <param name="document">Original document with the declaration.</param>
        /// <param name="localDeclarationStatement">Declaration statement to be moved.</param>
        /// <param name="canMovePastOtherDeclarationStatements">
        /// Flag indicating if the declaration statement can be moved past other declaration statements.
        /// If false, then this method will return false if there is another local declaration statement
        /// between <paramref name="localDeclarationStatement"/> and the statement with its first reference.
        /// For example, declaration statement for 'x' in the below code example can be moved after
        /// declaration statement for 'y' only if this flag is 'true':
        /// <code>
        ///     int x = 0;
        ///     int y = 0;
        ///     Console.WriteLine(x + y);
        /// </code>
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<bool> CanMoveDeclarationNearReferenceAsync(Document document, SyntaxNode localDeclarationStatement, bool canMovePastOtherDeclarationStatements, CancellationToken cancellationToken);

        /// <summary>
        /// Moves <paramref name="localDeclarationStatement"/> closer to its first reference. Only
        /// applicable if <see cref="CanMoveDeclarationNearReferenceAsync"/> returned
        /// <code>true</code>.  If not, then the original document will be returned unchanged.
        /// </summary>
        Task<Document> MoveDeclarationNearReferenceAsync(Document document, SyntaxNode localDeclarationStatement, CancellationToken cancellationToken);
    }
}
