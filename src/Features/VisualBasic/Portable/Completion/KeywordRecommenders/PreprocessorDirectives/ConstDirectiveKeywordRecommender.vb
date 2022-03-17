' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#Const" preprocessor directive
    ''' </summary>
    Friend Class ConstDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("#Const", VBFeaturesResources.Defines_a_conditional_compiler_constant_Conditional_compiler_constants_are_always_private_to_the_file_in_which_they_appear_The_expressions_used_to_initialize_them_can_contain_only_conditional_compiler_constants_and_literals))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.IsPreprocessorStartContext AndAlso Not context.SyntaxTree.IsEnumMemberNameContext(context),
                s_keywords,
                ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
