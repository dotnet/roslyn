' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations

    Friend Class CovarianceModifiersKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            ' No matter what, these can only happen after an Of or a comma
            If Not targetToken.IsKind(SyntaxKind.OfKeyword, SyntaxKind.CommaToken) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim parent = targetToken.Parent
            If parent Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim covarianceKeywords = {New RecommendedKeyword("In", VBFeaturesResources.Use_In_for_a_type_that_will_only_be_used_for_ByVal_arguments_to_functions),
                                      New RecommendedKeyword("Out", VBFeaturesResources.Use_Out_for_a_type_that_will_only_be_used_as_a_return_from_functions)}

            If parent.IsChildNode(Of DelegateStatementSyntax)(Function(declaration) declaration.TypeParameterList) Then
                Return covarianceKeywords
            ElseIf parent.IsChildNode(Of TypeStatementSyntax)(Function(declaration) declaration.TypeParameterList) Then
                If parent.GetAncestor(Of TypeStatementSyntax)().IsKind(SyntaxKind.InterfaceStatement) Then
                    Return covarianceKeywords
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
