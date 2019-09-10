' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If (context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext OrElse context.IsNameOfContext) AndAlso
                targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then

                ' Preselect the Me kewyord when the target type is the same 
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

                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_current_instance_of_a_class_or_structure_that_is_the_instance_in_which_the_code_is_running, matchPriority:=priority))
                End If

                Dim containingMember = targetToken.GetContainingMember()
                If TypeOf containingMember Is FieldDeclarationSyntax Then
                    Dim fieldDecl = DirectCast(containingMember, FieldDeclarationSyntax)
                    If Not fieldDecl.Modifiers.Any(SyntaxKind.SharedKeyword) Then
                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_current_instance_of_a_class_or_structure_that_is_the_instance_in_which_the_code_is_running))
                    End If
                End If
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=False, cancellationToken:=cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_current_instance_of_a_class_or_structure_that_is_the_instance_in_which_the_code_is_running))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
