' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Operator" keyword in member declaration contexts
    ''' </summary>
    Friend Class OperatorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim modifiers = context.ModifierCollectionFacts

            If context.SyntaxTree.IsDeclarationContextWithinTypeBlocks(context.Position, context.TargetToken, True, cancellationToken, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) AndAlso
               modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Operator) Then
                If modifiers.NarrowingOrWideningKeyword.Kind <> SyntaxKind.None Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Operator CType", VBFeaturesResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type_object_structure_class_or_interface_CType_Object_As_Expression_Object_As_Type_As_Type))
                Else
                    Return ImmutableArray.Create(New RecommendedKeyword("Operator", VBFeaturesResources.Declares_the_operator_symbol_operands_and_code_that_define_an_operator_procedure_on_a_class_or_structure))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
