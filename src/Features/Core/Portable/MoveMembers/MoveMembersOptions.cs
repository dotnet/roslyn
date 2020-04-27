// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal class MoveMembersOptions
    {
        public static readonly MoveMembersOptions Cancelled = new MoveMembersOptions(null!, default, false, null!, null!, isCancelled: true);

        public bool IsCancelled { get; }

        /// <summary>
        /// Destination of where members should be pulled up to.
        /// </summary>
        public INamedTypeSymbol Destination { get; }

        /// <summary>
        /// True if the destination is a new type symbol that will need to be created
        /// </summary>
        public bool DestinationIsNewType { get; }

        /// <summary>
        /// Indicate whether it would cause error if we directly move all members in MemberAnalysisResults up to destination.
        /// </summary>
        public bool OperationNeedsToDoExtraChanges { get; }

        /// <summary>
        /// All the members involved in this move operation,
        /// and the other changes needed so that this move operation won't cause error.
        /// </summary>
        public ImmutableArray<MemberAnalysisResult> MembersToMove { get; }

        /// <summary>
        /// Optional document that the members should be extracted to
        /// </summary>
        public DocumentId? DestinationDocument { get; }

        /// <summary>
        /// The type node for the original type being extracted/moved from
        /// </summary>
        public SyntaxNode FromTypeNode { get; }

        /// <summary>
        /// The original type that members are being moved from
        /// </summary>
        public INamedTypeSymbol OriginalType { get; }

        public string? NewFileName { get; }

        public MoveMembersOptions(
            INamedTypeSymbol destination,
            ImmutableArray<MemberAnalysisResult> membersToMove,
            bool isNewType,
            SyntaxNode fromTypeNode,
            INamedTypeSymbol originalType,
            DocumentId? destinationDocument = null,
            string? newFileName = null)
            : this(destination, membersToMove, isNewType, fromTypeNode, originalType, isCancelled: false, destinationDocument, newFileName)
        { }

        private MoveMembersOptions(
            INamedTypeSymbol destination,
            ImmutableArray<MemberAnalysisResult> membersToMove,
            bool isNewType,
            SyntaxNode fromTypeNode,
            INamedTypeSymbol originalType,
            bool isCancelled,
            DocumentId? destinationDocument = null,
            string? newFileName = null)
        {
            Destination = destination;
            MembersToMove = membersToMove;
            OperationNeedsToDoExtraChanges = membersToMove.Any(m => m.MoveMemberNeedsToDoExtraChanges);
            DestinationIsNewType = isNewType;
            DestinationDocument = destinationDocument;
            FromTypeNode = fromTypeNode;
            OriginalType = originalType;
            NewFileName = newFileName;
            IsCancelled = isCancelled;
        }
    }
}
