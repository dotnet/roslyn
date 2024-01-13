' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Assembly" and "Module" keyword for top-level attributes that may exist in a file.
    ''' </summary>
    Friend Class AttributeScopesKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(
                New RecommendedKeyword("Assembly", VBFeaturesResources.Specifies_that_an_attribute_at_the_beginning_of_a_source_file_applies_to_the_entire_assembly_Otherwise_the_attribute_will_apply_only_to_an_individual_programming_element_such_as_a_class_or_property),
                New RecommendedKeyword("Module", VBFeaturesResources.Specifies_that_an_attribute_at_the_beginning_of_a_source_file_applies_to_the_entire_module_Otherwise_the_attribute_will_apply_only_to_an_individual_programming_element_such_as_a_class_or_property))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsKind(SyntaxKind.LessThanToken) AndAlso
               targetToken.IsChildToken(Of AttributeListSyntax)(Function(block) block.LessThanToken) Then

                Dim attributeList = targetToken.Parent
                If attributeList.Parent.IsKind(SyntaxKind.AttributesStatement) Then
                    Return s_keywords
                End If

                If attributeList.GetAncestors(Of DeclarationStatementSyntax).Count() = 1 Then
                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
