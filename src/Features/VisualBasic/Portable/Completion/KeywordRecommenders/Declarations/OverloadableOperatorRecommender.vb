' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the various list of operators you can overload after the "Operator" keyword
    ''' </summary>
    Friend Class OverloadableOperatorRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If Not targetToken.IsKind(SyntaxKind.OperatorKeyword) OrElse
               Not targetToken.Parent.IsKind(SyntaxKind.OperatorStatement) Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim modifierFacts = context.ModifierCollectionFacts

            ' If we have a Widening or Narrowing declaration, then we must be a CType operator
            If modifierFacts.NarrowingOrWideningKeyword.Kind <> SyntaxKind.None Then
                Return ImmutableArray.Create(New RecommendedKeyword("CType", VBFeaturesResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type_object_structure_class_or_interface_CType_Object_As_Expression_Object_As_Type_As_Type))
            Else
                ' We could just be a normal name, so we list all possible options here. Dev10 allows you to type
                ' "Operator Narrowing", so we also list the Narrowing/Widening options as well.
                ' TODO: fix parser to actually deal with such stupidities like "Operator Narrowing"
                Return {"+", "-", "IsFalse", "IsTrue", "Not",
                        "*", "/", "\", "&", "^", ">>", "<<", "=", "<>", ">", ">=", "<", "<=", "And", "Like", "Mod", "Or", "Xor",
                        "Narrowing", "Widening"}.Select(Function(s) New RecommendedKeyword(s, GetToolTipForKeyword(s))).ToImmutableArray()
            End If
        End Function

        Private Shared Function GetToolTipForKeyword([operator] As String) As String
            Select Case [operator]
                Case "+"
                    Return VBFeaturesResources.Returns_the_sum_of_two_numbers_or_the_positive_value_of_a_numeric_expression
                Case "-"
                    Return VBFeaturesResources.Returns_the_difference_between_two_numeric_expressions_or_the_negative_value_of_a_numeric_expression
                Case "IsFalse"
                    Return VBFeaturesResources.Determines_whether_an_expression_is_false_If_instances_of_any_class_or_structure_will_be_used_in_an_OrElse_clause_you_must_define_IsFalse_on_that_class_or_structure
                Case "IsTrue"
                    Return VBFeaturesResources.Determines_whether_an_expression_is_true_If_instances_of_any_class_or_structure_will_be_used_in_an_OrElse_clause_you_must_define_IsTrue_on_that_class_or_structure
                Case "Not"
                    Return VBFeaturesResources.Performs_logical_negation_on_a_Boolean_expression_or_bitwise_negation_on_a_numeric_expression_result_Not_expression
                Case "*"
                    Return VBFeaturesResources.Multiplies_two_numbers_and_returns_the_product
                Case "/"
                    Return VBFeaturesResources.Divides_two_numbers_and_returns_a_floating_point_result
                Case "\"
                    Return VBFeaturesResources.Divides_two_numbers_and_returns_an_integer_result
                Case "&"
                    Return VBFeaturesResources.Generates_a_string_concatenation_of_two_expressions
                Case "^"
                    Return VBFeaturesResources.Raises_a_number_to_the_power_of_another_number
                Case ">>"
                    Return VBFeaturesResources.Performs_an_arithmetic_right_shift_on_a_bit_pattern
                Case "<<"
                    Return VBFeaturesResources.Performs_an_arithmetic_left_shift_on_a_bit_pattern
                Case "="
                    Return VBFeaturesResources.Compares_two_expressions_and_returns_True_if_they_are_equal_Otherwise_returns_False
                Case "<>"
                    Return VBFeaturesResources.Compares_two_expressions_and_returns_True_if_they_are_not_equal_Otherwise_returns_False
                Case ">"
                    Return VBFeaturesResources.Compares_two_expressions_and_returns_True_if_the_first_is_greater_than_the_second_Otherwise_returns_False
                Case ">="
                    Return VBFeaturesResources.Compares_two_expressions_and_returns_True_if_the_first_is_greater_than_or_equal_to_the_second_Otherwise_returns_False
                Case "<"
                    Return VBFeaturesResources.Compares_two_expressions_and_returns_True_if_the_first_is_less_than_the_second_Otherwise_returns_False
                Case "<="
                    Return VBFeaturesResources.Compares_two_expressions_and_returns_True_if_the_first_is_less_than_or_equal_to_the_second_Otherwise_returns_False
                Case "And"
                    Return VBFeaturesResources.Performs_a_logical_conjunction_on_two_Boolean_expressions_or_a_bitwise_conjunction_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_both_operands_evaluate_to_True_Both_expressions_are_always_evaluated_result_expression1_And_expression2
                Case "Like"
                    Return VBFeaturesResources.Compares_a_string_against_a_pattern_Wildcards_available_include_to_match_1_character_and_to_match_0_or_more_characters_result_string_Like_pattern
                Case "Mod"
                    Return VBFeaturesResources.Divides_two_numbers_and_returns_only_the_remainder_number1_Mod_number2
                Case "Or"
                    Return VBFeaturesResources.Performs_an_inclusive_logical_disjunction_on_two_Boolean_expressions_or_a_bitwise_disjunction_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_at_least_one_operand_evaluates_to_True_Both_expressions_are_always_evaluated_result_expression1_Or_expression2
                Case "Xor"
                    Return VBFeaturesResources.Performs_a_logical_exclusion_on_two_Boolean_expressions_or_a_bitwise_exclusion_on_two_numeric_expressions_For_Boolean_expressions_returns_True_if_exactly_one_of_the_expressions_evaluates_to_True_Both_expressions_are_always_evaluated_result_expression1_Xor_expression2
                Case "Narrowing"
                    Return VBFeaturesResources.Indicates_that_a_conversion_operator_CType_converts_a_class_or_structure_to_a_type_that_might_not_be_able_to_hold_some_of_the_possible_values_of_the_original_class_or_structure
                Case "Widening"
                    Return VBFeaturesResources.Indicates_that_a_conversion_operator_CType_converts_a_class_or_structure_to_a_type_that_can_hold_all_possible_values_of_the_original_class_or_structure
                Case Else
                    Return String.Empty
            End Select
        End Function
    End Class
End Namespace
