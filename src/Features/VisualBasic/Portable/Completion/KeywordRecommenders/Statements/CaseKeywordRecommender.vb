' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Case" and possibly "Case Else" keyword inside a Select block
    ''' </summary>
    Friend Class CaseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            ' Are we after "Select" for "Select Case"?
            If targetToken.Kind = SyntaxKind.SelectKeyword AndAlso
               Not targetToken.Parent.IsKind(SyntaxKind.SelectClause) AndAlso
               Not context.FollowsEndOfStatement Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Case", VBFeaturesResources.CaseKeywordToolTip))
            End If

            ' A "Case" keyword must be in a Select block, and exists either where a regular executable statement can go
            ' or the special case of being immediately after the Select Case
            If Not context.IsInStatementBlockOfKind(SyntaxKind.SelectBlock) OrElse
               Not (context.IsMultiLineStatementContext OrElse context.IsAfterStatementOfKind(SyntaxKind.SelectStatement)) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim selectStatement = targetToken.GetAncestor(Of SelectBlockSyntax)()
            Dim validKeywords As New List(Of RecommendedKeyword)

            ' We can do "Case" as long as we're not after a "Case Else"
            Dim caseElseBlock = selectStatement.CaseBlocks.FirstOrDefault(Function(caseBlock) caseBlock.CaseStatement.Kind = SyntaxKind.CaseElseStatement)
            If caseElseBlock Is Nothing OrElse targetToken.SpanStart < caseElseBlock.SpanStart Then
                validKeywords.Add(New RecommendedKeyword("Case", VBFeaturesResources.CaseKeywordToolTip))
            End If

            ' We can do a "Case Else" as long as we're the last one and we don't already have one.
            ' We exclude any partial case keywords the parser is creating (possibly because of user typing)
            Dim lastBlock = selectStatement.CaseBlocks.LastOrDefault(Function(caseBlock) Not caseBlock.CaseStatement.CaseKeyword.IsMissing)
            If caseElseBlock Is Nothing AndAlso (lastBlock Is Nothing OrElse targetToken.SpanStart > lastBlock.SpanStart) Then
                validKeywords.Add(New RecommendedKeyword("Case Else", VBFeaturesResources.CaseElseKeywordToolTip))
            End If

            Return validKeywords
        End Function
    End Class
End Namespace
