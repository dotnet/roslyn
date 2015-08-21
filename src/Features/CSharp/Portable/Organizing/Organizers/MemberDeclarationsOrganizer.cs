// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    internal static partial class MemberDeclarationsOrganizer
    {
        public static SyntaxList<MemberDeclarationSyntax> Organize(
            SyntaxList<MemberDeclarationSyntax> members,
            CancellationToken cancellationToken)
        {
            // Break the list of members up into groups based on the PP 
            // directives between them.
            var groups = members.SplitNodesOnPreprocessorBoundaries(cancellationToken);

            // Go into each group and organize them.  We'll then have a list of 
            // lists.  Flatten that list and return that.
            var sortedGroups = groups.Select(OrganizeMemberGroup).Flatten().ToList();

            if (sortedGroups.SequenceEqual(members))
            {
                return members;
            }

            return sortedGroups.ToSyntaxList();
        }

        private static void TransferTrivia<TSyntaxNode>(
            IList<TSyntaxNode> originalList,
            IList<TSyntaxNode> finalList) where TSyntaxNode : SyntaxNode
        {
            Contract.Requires(originalList.Count == finalList.Count);

            if (originalList.Count >= 2)
            {
                // Ok, we wanted to reorder the list.  But we're definitely not done right now. While
                // most of the list will look fine, we will have issues with the first node.  First,
                // we don't want to move any pp directives or banners that are on the first node.
                // Second, it can often be the case that the node doesn't even have any trivia.  We
                // want to follow the user's style.  So we find the node that was in the index that
                // the first node moved to, and we attempt to keep an appropriate amount of
                // whitespace based on that.

                // If the first node didn't move, then we don't need to do any of this special fixup
                // logic.
                if (originalList[0] != finalList[0])
                {
                    // First. Strip any pp directives or banners on the first node.  They have to
                    // move to the first node in the final list.
                    CopyBanner(originalList, finalList);

                    // Now, we need to fix up the first node wherever it is in the final list.  We
                    // need to strip it of its banner, and we need to add additional whitespace to
                    // match the user's style

                    FixupOriginalFirstNode(originalList, finalList);
                }
            }
        }

        private static void FixupOriginalFirstNode<TSyntaxNode>(IList<TSyntaxNode> originalList, IList<TSyntaxNode> finalList) where TSyntaxNode : SyntaxNode
        {
            // Now, find the original node in the final list.
            var originalFirstNode = originalList[0];
            var indexInFinalList = finalList.IndexOf(originalFirstNode);

            // Find the initial node we had at that same index.
            var originalNodeAtThatIndex = originalList[indexInFinalList];

            // If that node had blank lines above it, then place that number of blank
            // lines before the first node in the final list.
            var blankLines = originalNodeAtThatIndex.GetLeadingBlankLines();

            originalFirstNode = originalFirstNode.GetNodeWithoutLeadingBannerAndPreprocessorDirectives()
                .WithPrependedLeadingTrivia(blankLines);

            finalList[indexInFinalList] = originalFirstNode;
        }

        private static void CopyBanner<TSyntaxNode>(
            IList<TSyntaxNode> originalList,
            IList<TSyntaxNode> finalList) where TSyntaxNode : SyntaxNode
        {
            // First. Strip any pp directives or banners on the first node.  They
            // have to stay at the top of the list.
            var banner = originalList[0].GetLeadingBannerAndPreprocessorDirectives();

            // Now, we want to remove any blank lines from the new first node and then 
            // reattach the banner. 
            var finalFirstNode = finalList[0];
            finalFirstNode = finalFirstNode.GetNodeWithoutLeadingBlankLines();
            finalFirstNode = finalFirstNode.WithLeadingTrivia(banner.Concat(finalFirstNode.GetLeadingTrivia()));

            // Place the updated first node back in the result list
            finalList[0] = finalFirstNode;
        }

        private static IList<MemberDeclarationSyntax> OrganizeMemberGroup(IList<MemberDeclarationSyntax> members)
        {
            if (members.Count > 1)
            {
                var initialList = new List<MemberDeclarationSyntax>(members);

                var finalList = initialList.OrderBy(new Comparer()).ToList();

                if (!finalList.SequenceEqual(initialList))
                {
                    // Ok, we wanted to reorder the list.  But we're definitely not done right now.
                    // While most of the list will look fine, we will have issues with the first 
                    // node.  First, we don't want to move any pp directives or banners that are on
                    // the first node.  Second, it can often be the case that the node doesn't even
                    // have any trivia.  We want to follow the user's style.  So we find the node that
                    // was in the index that the first node moved to, and we attempt to keep an
                    // appropriate amount of whitespace based on that.
                    TransferTrivia(initialList, finalList);

                    return finalList;
                }
            }

            return members;
        }
    }
}
