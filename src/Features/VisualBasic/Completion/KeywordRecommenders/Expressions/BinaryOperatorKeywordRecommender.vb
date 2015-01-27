' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends binary infix operators that are English text, like "AndAlso", "OrElse", "Like", etc.
    ''' </summary>
    Friend Class BinaryOperatorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Friend Shared ReadOnly KeywordList As RecommendedKeyword() = {
            New RecommendedKeyword("And", VBFeaturesResources.AndKeywordToolTip),
            New RecommendedKeyword("AndAlso", VBFeaturesResources.AndAlsoKeywordToolTip),
            New RecommendedKeyword("Or", VBFeaturesResources.OrKeywordToolTip),
            New RecommendedKeyword("OrElse", VBFeaturesResources.OrElseKeywordToolTip),
            New RecommendedKeyword("Is", VBFeaturesResources.IsKeywordToolTip),
            New RecommendedKeyword("IsNot", VBFeaturesResources.IsNotKeywordToolTip),
            New RecommendedKeyword("Mod", VBFeaturesResources.ModKeywordToolTip),
            New RecommendedKeyword("Like", VBFeaturesResources.LikeKeywordToolTip),
            New RecommendedKeyword("Xor", VBFeaturesResources.XorKeywordToolTip)}

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If IsBinaryOperatorContext(context, cancellationToken) Then
                Return KeywordList
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function IsBinaryOperatorContext(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            If context.FollowsEndOfStatement Then
                Return False
            End If

            Dim token = context.TargetToken

            ' Very specific case edge case when the identifier is From or Aggregate. In that case, we'll
            ' only show the binary operator keywords if "From" or "Aggregate" binds to a symbol. In that
            ' way, we can distinguish between the two following cases:
            '
            ' 1.
            ' Dim q = From |
            '
            ' 2.
            ' Dim From = 0
            ' Dim q = From |

            Dim identifierName = TryCast(token.Parent, IdentifierNameSyntax)
            If identifierName IsNot Nothing Then
                Dim text = token.ToString()
                If (SyntaxFacts.GetContextualKeywordKind(text) = SyntaxKind.FromKeyword OrElse SyntaxFacts.GetContextualKeywordKind(text) = SyntaxKind.AggregateKeyword) Then
                    Dim symbol = context.SemanticModel.GetSymbolInfo(identifierName).Symbol
                    If symbol Is Nothing Then
                        Return False
                    End If
                End If
            End If

            ' Don't show binary operator keywords in an incomplete Using block
            ' Using foo |
            Dim usingStatement = token.GetAncestor(Of UsingStatementSyntax)()
            If usingStatement IsNot Nothing AndAlso usingStatement.Expression IsNot Nothing AndAlso Not usingStatement.Expression.IsMissing Then
                If usingStatement.Expression Is token.Parent Then
                    Return False
                End If
            End If

            ' As a policy, we'll not show them after an object or collection initializer, since we
            ' really just want to show "From" or "With"
            If token.IsFollowingCompleteAsNewClause() OrElse
               token.IsFollowingCompleteObjectCreationInitializer() Then
                Return False
            End If

            ' Binary operators are legal inside a join expression, but we'll show
            ' just "Equals" to better guide the user on what they should be
            ' typing
            If context.SyntaxTree.IsFollowingCompleteExpression(Of JoinConditionSyntax)(
               context.Position, context.TargetToken, Function(j) j.Left, cancellationToken) Then
                Return False
            End If

            ' Binary operators are allowed in cases like
            '
            '    From num In { 1, 2, 3 } Group By a = num |
            '
            ' but we will choose to exclude them so the user gets better hints of what they have to
            ' type next in the query
            If context.SyntaxTree.IsFollowingCompleteExpression(Of ExpressionRangeVariableSyntax)(
               context.Position, context.TargetToken, Function(j) j.Expression, cancellationToken) Then
                Return False
            End If

            ' Some operators (And, Or) are technically legal after an AddressOf expression, but
            ' that's unnecessarily pedantic
            If context.SyntaxTree.IsFollowingCompleteExpression(Of UnaryExpressionSyntax)(context.Position, context.TargetToken,
                Function(u As UnaryExpressionSyntax)
                    If u.Kind = SyntaxKind.AddressOfExpression Then
                        Return u
                    Else
                        Return Nothing
                    End If
                End Function, cancellationToken) Then

                Return False
            End If

            ' In either of these cases:
            '
            '     Dim x(0 |
            '     ReDim y(0 |
            '
            ' it's legal to write a binary operator, but in all probability the user wants to write
            ' To. Note that if they are writing To then it must be a literal zero, so we'll restrict
            ' to that case
            If token.Kind = SyntaxKind.IntegerLiteralToken AndAlso CInt(token.Value) = 0 Then
                If token.Parent.IsParentKind(SyntaxKind.SimpleArgument) Then
                    Dim argumentList = token.GetAncestor(Of ArgumentListSyntax)()
                    If argumentList.Parent IsNot Nothing AndAlso (TypeOf argumentList.Parent.Parent Is ReDimStatementSyntax OrElse
                                                                  TypeOf argumentList.Parent.Parent Is VariableDeclaratorSyntax) Then
                        Return False
                    End If
                End If
            End If

            ' The expression in an Add/RemoveHandler which specifies the event is just an event, and
            ' thus can't get operators applied to it
            If context.SyntaxTree.IsFollowingCompleteExpression(Of AddRemoveHandlerStatementSyntax)(
               context.Position, context.TargetToken, Function(h) h.EventExpression, cancellationToken) Then
                Return False
            End If

            ' Exclude from For statements:
            '       For i = 1 |
            ' This is legal but is not a good experience in most cases
            If context.SyntaxTree.IsFollowingCompleteExpression(Of ForStatementSyntax)(context.Position, context.TargetToken, Function(forStatement) forStatement.FromValue, cancellationToken) Then
                Return False
            End If

            Return context.SyntaxTree.IsFollowingCompleteExpression(Of ExpressionSyntax)(context.Position, context.TargetToken,
               Function(e)
                   If context.SyntaxTree.IsExpressionContext(e.SpanStart, cancellationToken, context.SemanticModel) Then
                       Return e
                   Else
                       Return Nothing
                   End If
               End Function, cancellationToken)
        End Function
    End Class
End Namespace
