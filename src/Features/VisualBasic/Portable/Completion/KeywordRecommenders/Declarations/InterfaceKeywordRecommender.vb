' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Interface" keyword in type declaration contexts
    ''' </summary>
    Friend Class InterfaceKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsTypeDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts

                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Interface) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Interface", VBFeaturesResources.Declares_the_name_of_an_interface_and_the_definitions_of_the_members_of_the_interface))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
