' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Friend Shared ReadOnly KeywordList As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("And", VBFeaturesResources.Performs_a_logical_conjunction_on_two_Boolean_expressions_or_a_bitwise_conjunction_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_both_operands_evaluate_to_True_Both_expressions_are_always_evaluated_result_expression1_And_expression2),
            New RecommendedKeyword("AndAlso", VBFeaturesResources.Performs_a_short_circuit_logical_conjunction_on_two_expressions_Returns_True_if_both_operands_evaluate_to_True_If_the_first_expression_evaluates_to_False_the_second_is_not_evaluated_result_expression1_AndAlso_expression2),
            New RecommendedKeyword("Or", VBFeaturesResources.Performs_an_inclusive_logical_disjunction_on_two_Boolean_expressions_or_a_bitwise_disjunction_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_at_least_one_operand_evaluates_to_True_Both_expressions_are_always_evaluated_result_expression1_Or_expression2),
            New RecommendedKeyword("OrElse", VBFeaturesResources.Performs_short_circuit_inclusive_logical_disjunction_on_two_expressions_Returns_True_if_either_operand_evaluates_to_True_If_the_first_expression_evaluates_to_True_the_second_expression_is_not_evaluated_result_expression1_OrElse_expression2),
            New RecommendedKeyword("Is", VBFeaturesResources.Compares_two_object_reference_variables_and_returns_True_if_the_objects_are_equal_result_object1_Is_object2),
            New RecommendedKeyword("IsNot", VBFeaturesResources.Compares_two_object_reference_variables_and_returns_True_if_the_objects_are_not_equal_result_object1_IsNot_object2),
            New RecommendedKeyword("Mod", VBFeaturesResources.Divides_two_numbers_and_returns_only_the_remainder_number1_Mod_number2),
            New RecommendedKeyword("Like", VBFeaturesResources.Compares_a_string_against_a_pattern_Wildcards_available_include_to_match_1_character_and_to_match_0_or_more_characters_result_string_Like_pattern),
            New RecommendedKeyword("Xor", VBFeaturesResources.Performs_a_logical_exclusion_on_two_Boolean_expressions_or_a_bitwise_exclusion_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_exactly_one_of_the_expressions_evaluates_to_True_Both_expressions_are_always_evaluated_result_expression1_Xor_expression2))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If IsBinaryOperatorContext(context, cancellationToken) Then
                Return KeywordList
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function IsBinaryOperatorContext(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
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
                    Dim symbol = context.SemanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol
                    If symbol Is Nothing Then
                        Return False
                    End If
                End If
            End If

            ' Don't show binary operator keywords in an incomplete Using block
            ' Using goo |
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
