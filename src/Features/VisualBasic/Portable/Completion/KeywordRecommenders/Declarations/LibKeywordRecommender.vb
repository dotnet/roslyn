' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Lib" keyword in external method declarations.
    ''' </summary>
    Friend Class LibKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.TargetToken.IsChildToken(Of DeclareStatementSyntax)(Function(declaration) declaration.Identifier) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Lib", VBFeaturesResources.Introduces_a_clause_that_identifies_the_external_file_DLL_or_code_resource_containing_an_external_procedure))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
