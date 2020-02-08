﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Namespace" keyword in type declaration contexts
    ''' </summary>
    Friend Class NamespaceKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.SyntaxTree.IsDeclarationContextWithinTypeBlocks(context.Position, context.TargetToken, False, cancellationToken, SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock) Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Class) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Namespace", VBFeaturesResources.Declares_the_name_of_a_namespace_and_causes_the_source_code_following_the_declaration_to_be_compiled_within_that_namespace))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
