' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Module" keyword in type declaration contexts
    ''' </summary>
    Friend Class ModuleKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.SyntaxTree.IsDeclarationContextWithinTypeBlocks(context.Position, context.TargetToken, True, cancellationToken, SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock) Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Module) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Module", VBFeaturesResources.Specifies_that_an_attribute_at_the_beginning_of_a_source_file_applies_to_the_entire_module_Otherwise_the_attribute_will_apply_only_to_an_individual_programming_element_such_as_a_class_or_property))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
