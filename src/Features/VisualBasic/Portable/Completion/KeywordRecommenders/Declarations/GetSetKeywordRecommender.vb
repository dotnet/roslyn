' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)

            ' If we have modifiers which exclude it, then definitely not
            Dim modifiers = context.ModifierCollectionFacts
            If Not modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Accessor) Then
                Return Enumerable.Empty(Of RecommendedKeyword)()
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
                recommendations.Add(New RecommendedKeyword("Get", VBFeaturesResources.GetPropertyKeywordToolTip))
            End If

            If setAllowed Then
                recommendations.Add(New RecommendedKeyword("Set", VBFeaturesResources.SetPropertyKeywordToolTip))
            End If

            Return recommendations
        End Function
    End Class
End Namespace
