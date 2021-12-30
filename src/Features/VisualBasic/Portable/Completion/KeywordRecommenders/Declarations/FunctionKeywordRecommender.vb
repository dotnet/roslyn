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
    ''' Recommends the "Function" keyword in member declaration contexts
    ''' </summary>
    Friend Class FunctionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Function", VBFeaturesResources.Declares_the_name_parameters_and_code_that_define_a_Function_procedure_that_is_a_procedure_that_returns_a_value_to_the_calling_code))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, CancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsTypeMemberDeclarationKeywordContext OrElse context.IsInterfaceMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.OverridableSharedOrPartialKeyword.Kind = SyntaxKind.PartialKeyword Then
                    Return ImmutableArray(Of RecommendedKeyword).Empty
                End If

                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.IteratorFunction) Then
                    Return s_keywords
                End If
            End If

            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKindOrHasMatchingText(SyntaxKind.ExitKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.FunctionBlock, SyntaxKind.MultiLineFunctionLambdaExpression) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
