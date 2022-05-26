' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations

    Friend Class OfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            If Not targetToken.IsKind(SyntaxKind.OpenParenToken) Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim methodDeclaration = targetToken.GetAncestor(Of MethodStatementSyntax)()
            If methodDeclaration IsNot Nothing Then
                If methodDeclaration.TypeParameterList IsNot Nothing Then
                    If targetToken = methodDeclaration.TypeParameterList.OpenParenToken Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                    End If
                ElseIf methodDeclaration.ParameterList IsNot Nothing Then
                    ' If we don't have a TypeParametersOpt, then we might be in a place where it's ambiguous where we are.
                    ' For example, typing Sub Goo(|, it's not clear if that's a TypeParameters block I'm in or a regular
                    ' block. The parser chooses the sane choice of calling that a regular parameters block until it knows
                    ' otherwise.
                    If targetToken = methodDeclaration.ParameterList.OpenParenToken Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                    End If
                End If
            End If

            Dim implementsClause = targetToken.GetAncestor(Of ImplementsClauseSyntax)
            If implementsClause IsNot Nothing Then
                If targetToken.IsKind(SyntaxKind.OpenParenToken) AndAlso targetToken.Parent.IsKind(SyntaxKind.TypeArgumentList) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                End If
            End If

            Dim inheritsStatement = targetToken.GetAncestor(Of InheritsStatementSyntax)
            If inheritsStatement IsNot Nothing Then
                If targetToken.IsKind(SyntaxKind.OpenParenToken) AndAlso targetToken.Parent.IsKind(SyntaxKind.TypeArgumentList) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                End If
            End If

            Dim delegateDeclaration = targetToken.GetAncestor(Of DelegateStatementSyntax)()
            If delegateDeclaration IsNot Nothing Then
                If delegateDeclaration.TypeParameterList IsNot Nothing Then
                    If targetToken = delegateDeclaration.TypeParameterList.OpenParenToken Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                    End If
                ElseIf delegateDeclaration.ParameterList IsNot Nothing Then
                    If targetToken = delegateDeclaration.ParameterList.OpenParenToken Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                    End If
                End If
            End If

            Dim typeDeclaration = targetToken.GetAncestor(Of TypeStatementSyntax)()
            If typeDeclaration IsNot Nothing AndAlso typeDeclaration.IsKind(SyntaxKind.ClassStatement, SyntaxKind.InterfaceStatement, SyntaxKind.StructureStatement) Then
                If typeDeclaration.TypeParameterList IsNot Nothing Then
                    If targetToken = typeDeclaration.TypeParameterList.OpenParenToken Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                    End If
                End If
            End If

            ' Cases:
            '  Dim f As New Goo(|
            '  Goo(|
            Dim argumentList = targetToken.GetAncestor(Of ArgumentListSyntax)()
            If argumentList IsNot Nothing Then
                ' Ensure it's not "Dim F(|" unless creating a generic delegate
                If Not argumentList.HasAncestor(Of ModifiedIdentifierSyntax)() AndAlso
                   targetToken = argumentList.OpenParenToken AndAlso
                   (Not context.IsDelegateCreationContext() OrElse IsGenericDelegateCreationExpression(targetToken, context.SemanticModel, cancellationToken)) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                End If
            End If

            ' Case: Dim f As Goo(|
            Dim arrayRankSpecifier = targetToken.GetAncestor(Of ArrayRankSpecifierSyntax)()
            If arrayRankSpecifier IsNot Nothing Then
                If targetToken = arrayRankSpecifier.OpenParenToken Then
                    Dim arrayType = TryCast(arrayRankSpecifier.Parent, ArrayTypeSyntax)
                    If arrayType IsNot Nothing AndAlso IsPartiallyTypedGenericName(arrayType.ElementType, context.SemanticModel) Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Of", VBFeaturesResources.Identifies_a_type_parameter_on_a_generic_class_structure_interface_delegate_or_procedure))
                    End If
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function IsPartiallyTypedGenericName(type As TypeSyntax, semanticModel As SemanticModel) As Boolean
            Dim symbols = semanticModel.LookupNamespacesAndTypes(
                position:=type.SpanStart,
                name:=type.ToString())

            Return symbols.OfType(Of INamedTypeSymbol)() _
                          .Where(Function(nt) nt.IsGenericType) _
                          .Any()
        End Function

        Private Shared Function IsGenericDelegateCreationExpression(token As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim objectCreationExpression = token.GetAncestor(Of ObjectCreationExpressionSyntax)()
            If objectCreationExpression IsNot Nothing Then
                Dim type = objectCreationExpression.Type
                If type IsNot Nothing Then
                    Return semanticModel.LookupName(type, namespacesAndTypesOnly:=True, cancellationToken:=cancellationToken) _
                                        .OfType(Of INamedTypeSymbol)() _
                                        .Where(Function(nt) nt.IsDelegateType AndAlso nt.Arity > 0) _
                                        .Any()
                End If
            End If

            Return False
        End Function
    End Class
End Namespace
