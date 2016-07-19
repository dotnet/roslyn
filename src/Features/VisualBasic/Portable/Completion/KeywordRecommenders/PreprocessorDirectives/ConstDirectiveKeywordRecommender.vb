' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#Const" preprocessor directive
    ''' </summary>
    Friend Class ConstDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreprocessorStartContext AndAlso Not context.SyntaxTree.IsEnumMemberNameContext(context) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("#Const", VBFeaturesResources.Defines_a_conditional_compiler_constant_Conditional_compiler_constants_are_always_private_to_the_file_in_which_they_appear_The_expressions_used_to_initialize_them_can_contain_only_conditional_compiler_constants_and_literals))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
