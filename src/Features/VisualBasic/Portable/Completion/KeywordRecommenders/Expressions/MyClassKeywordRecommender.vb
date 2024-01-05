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
    ''' Recommends the "MyClass" keyword.
    ''' </summary>
    Friend Class MyClassKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyClassKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_class_instance_members_as_originally_implemented_ignoring_any_derived_class_overrides))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If (context.IsAnyExpressionContext OrElse context.IsStatementContext OrElse context.IsNameOfContext) AndAlso
               targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                ' This isn't valid in shared methods
                Dim methodBlock = targetToken.GetAncestor(Of MethodBlockBaseSyntax)()

                If methodBlock IsNot Nothing AndAlso methodBlock.BlockStatement.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.SharedKeyword) Then
                    Return ImmutableArray(Of RecommendedKeyword).Empty
                End If

                Return s_keywords
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=False) Then
                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
