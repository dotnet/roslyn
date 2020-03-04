' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Function" and "Sub" keywords in external method declarations.
    ''' </summary>
    Friend Class DelegateSubFunctionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.TargetToken.IsChildToken(Of DelegateStatementSyntax)(Function(delegateDeclaration) delegateDeclaration.DelegateKeyword) Then
                Return {New RecommendedKeyword("Function", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Function_procedure_that_is_a_procedure_that_returns_a_value_to_the_calling_code),
                        New RecommendedKeyword("Sub", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Sub_procedure_that_is_a_procedure_that_does_not_return_a_value_to_the_calling_code)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
