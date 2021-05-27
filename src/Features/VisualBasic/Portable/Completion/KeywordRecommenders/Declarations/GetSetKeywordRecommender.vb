' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Get" and "Set" keyword in property declarations.
    ''' </summary>
    Friend Class GetSetKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            ' If we have modifiers which exclude it, then definitely not
            Dim modifiers = context.ModifierCollectionFacts
            If Not modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Accessor) Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            ' Are we following the property declaration?
            Dim previousToken = targetToken
            Do While previousToken.IsModifier()
                previousToken = previousToken.GetPreviousToken()
            Loop

            Dim propertyBlock = previousToken.GetAncestor(Of PropertyBlockSyntax)()
            Dim propertyDeclaration = previousToken.GetAncestor(Of PropertyStatementSyntax)()
            Dim accessorBlock = previousToken.GetAncestors(Of SyntaxNode)().FirstOrDefault(Function(ancestor) ancestor.IsKind(SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock))

            If propertyBlock IsNot Nothing AndAlso propertyDeclaration Is Nothing Then
                propertyDeclaration = propertyBlock.PropertyStatement
            End If

            Dim getAllowed = False
            Dim setAllowed = False

            If propertyDeclaration IsNot Nothing Then
                If Not propertyDeclaration.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.ReadOnlyKeyword) Then
                    setAllowed = True
                End If

                If Not propertyDeclaration.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.WriteOnlyKeyword) Then
                    getAllowed = True
                End If
            End If

            ' If we're already after a previous accessor, then exclude it
            If accessorBlock.IsKind(SyntaxKind.GetAccessorBlock) Then
                getAllowed = False
            End If

            If accessorBlock.IsKind(SyntaxKind.SetAccessorBlock) Then
                setAllowed = False
            End If

            Dim recommendations As New List(Of RecommendedKeyword)()

            If getAllowed Then
                recommendations.Add(New RecommendedKeyword("Get", VBFeaturesResources.Declares_a_Get_property_procedure_that_is_used_to_return_the_current_value_of_a_property))
            End If

            If setAllowed Then
                recommendations.Add(New RecommendedKeyword("Set", VBFeaturesResources.Declares_a_Set_property_procedure_that_is_used_to_assign_a_value_to_a_property))
            End If

            Return recommendations.ToImmutableArray()
        End Function
    End Class
End Namespace
