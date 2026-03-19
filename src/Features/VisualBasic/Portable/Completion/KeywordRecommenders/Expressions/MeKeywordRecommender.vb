' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "Me" keyword.
    ''' </summary>
    Friend Class MeKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If (context.IsAnyExpressionContext OrElse context.IsStatementContext OrElse context.IsNameOfContext) AndAlso
                targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then

                ' Preselect the Me keyword when the target type is the same
                ' as the enclosing type symbol of the body we're typing in

                Dim priority = MatchPriority.Default
                Dim enclosingType = context.SemanticModel.GetEnclosingNamedType(context.Position, cancellationToken)
                If enclosingType IsNot Nothing AndAlso context.InferredTypes.Any(Function(t) Equals(t, enclosingType)) Then
                    priority = SymbolMatchPriority.Keyword
                End If

                If targetToken.GetContainingMemberBlockBegin().TypeSwitch(
                    Function(methodBase As MethodBaseSyntax) Not methodBase.Modifiers.Any(SyntaxKind.SharedKeyword),
                    Function(propertyStatement As PropertyStatementSyntax) Not propertyStatement.Modifiers.Any(SyntaxKind.SharedKeyword),
                    Function(eventStatement As EventStatementSyntax) Not eventStatement.Modifiers.Any(SyntaxKind.SharedKeyword)) Then

                    Return ImmutableArray.Create(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_current_instance_of_a_class_or_structure_that_is_the_instance_in_which_the_code_is_running, matchPriority:=priority))
                End If

                Dim containingMember = targetToken.GetContainingMember()
                If TypeOf containingMember Is FieldDeclarationSyntax Then
                    Dim fieldDecl = DirectCast(containingMember, FieldDeclarationSyntax)
                    If Not fieldDecl.Modifiers.Any(SyntaxKind.SharedKeyword) Then
                        Return ImmutableArray.Create(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_current_instance_of_a_class_or_structure_that_is_the_instance_in_which_the_code_is_running))
                    End If
                End If
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=False) Then
                Return ImmutableArray.Create(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_current_instance_of_a_class_or_structure_that_is_the_instance_in_which_the_code_is_running))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
