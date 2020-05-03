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
    Friend Class ExternalSubFunctionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.DeclareKeyword, SyntaxKind.AnsiKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.AutoKeyword) AndAlso
               targetToken.GetAncestor(Of DeclareStatementSyntax)() IsNot Nothing Then
                Return {New RecommendedKeyword("Function", VBFeaturesResources.Specifies_that_the_external_procedure_being_referenced_in_the_Declare_statement_is_a_Function),
                        New RecommendedKeyword("Sub", VBFeaturesResources.Specifies_that_the_external_procedure_being_referenced_in_the_Declare_statement_is_a_Sub)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
