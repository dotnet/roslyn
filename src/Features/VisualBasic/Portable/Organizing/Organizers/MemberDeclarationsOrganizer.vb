' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing.Organizers
    Friend Partial Class MemberDeclarationsOrganizer
        Private Sub New()
        End Sub

        Public Shared Function Organize(members As SyntaxList(Of StatementSyntax),
                                        cancellationToken As CancellationToken) As SyntaxList(Of StatementSyntax)

            ' Break the list of members up into groups based on the PP 
            ' directives between them.
            Dim groups = members.SplitNodesOnPreprocessorBoundaries(cancellationToken)

            ' Go into each group and organize them.  We'll then have a list of 
            ' lists.  Flatten that list and return that.
            Dim sortedGroups = groups.Select(AddressOf OrganizeMemberGroup).Flatten().ToList()

            If sortedGroups.SequenceEqual(members) Then
                Return members
            End If

            Return SyntaxFactory.List(sortedGroups)
        End Function

        Private Shared Sub TransferTrivia(Of TSyntaxNode As SyntaxNode)(
            originalList As IList(Of TSyntaxNode),
            finalList As IList(Of TSyntaxNode))
            Debug.Assert(originalList.Count = finalList.Count)

            If originalList.Count >= 2 Then
                ' Ok, we wanted to reorder the list.  But we're definitely not done right now. While
                ' most of the list will look fine, we will have issues with the first node.  First,
                ' we don't want to move any pp directives or banners that are on the first node.
                ' Second, it can often be the case that the node doesn't even have any trivia.  We
                ' want to follow the user's style.  So we find the node that was in the index that
                ' the first node moved to, and we attempt to keep an appropriate amount of
                ' whitespace based on that.
                '
                ' If the first node didn't move, then we don't need to do any of this special fixup
                ' logic.
                If originalList(0) IsNot finalList(0) Then
                    ' First. Strip any pp directives or banners on the first node.  They have to
                    ' move to the first node in the final list.
                    CopyBanner(originalList, finalList)

                    ' Now, we need to fix up the first node wherever it is in the final list.  We
                    ' need to strip it of its banner, and we need to add additional whitespace to
                    ' match the user's style
                    FixupOriginalFirstNode(Of TSyntaxNode)(originalList, finalList)
                End If
            End If
        End Sub

        Private Shared Sub FixupOriginalFirstNode(Of TSyntaxNode As SyntaxNode)(
            originalList As IList(Of TSyntaxNode),
            finalList As IList(Of TSyntaxNode))

            ' Now, find the original node in the final list.
            Dim originalFirstNode = originalList(0)
            Dim indexInFinalList = finalList.IndexOf(originalFirstNode)

            ' Find the initial node we had at that same index.
            Dim originalNodeAtThatIndex = originalList(indexInFinalList)

            ' If that node had blank lines above it, then place that number of blank
            ' lines before the first node in the final list.
            Dim blankLines = originalNodeAtThatIndex.GetLeadingBlankLines()

            originalFirstNode = originalFirstNode.GetNodeWithoutLeadingBannerAndPreprocessorDirectives().
                WithPrependedLeadingTrivia(blankLines)

            finalList(indexInFinalList) = originalFirstNode
        End Sub

        Private Shared Sub CopyBanner(Of TSyntaxNode As SyntaxNode)(
            originalList As IList(Of TSyntaxNode),
            finalList As IList(Of TSyntaxNode))

            ' First. Strip any pp directives or banners on the first node.  They
            ' have to stay at the top of the list.
            Dim banner = originalList(0).GetLeadingBannerAndPreprocessorDirectives()

            ' Now, we want to remove any blank lines from the new first node and then 
            ' reattach the banner. 
            Dim finalFirstNode = finalList(0)
            finalFirstNode = finalFirstNode.GetNodeWithoutLeadingBlankLines()
            finalFirstNode = finalFirstNode.WithLeadingTrivia(banner.Concat(finalFirstNode.GetLeadingTrivia()))

            ' Place the updated first node back in the result list
            finalList(0) = finalFirstNode
        End Sub

        Private Shared Function OrganizeMemberGroup(members As IList(Of StatementSyntax)) As IList(Of StatementSyntax)
            If members.Count > 1 Then
                Dim originalList = New List(Of StatementSyntax)(members)

                Dim finalList = originalList.OrderBy(New Comparer()).ToList()

                If Not finalList.SequenceEqual(originalList) Then
                    TransferTrivia(originalList, finalList)

                    Return finalList
                End If
            End If

            Return members
        End Function
    End Class
End Namespace
