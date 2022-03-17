' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#Disable" preprocessor directive
    ''' </summary>
    Friend Class WarningDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsPreprocessorStartContext AndAlso Not context.SyntaxTree.IsEnumMemberNameContext(context) Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("#Enable Warning", VBFeaturesResources.Enables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line),
                    New RecommendedKeyword("#Disable Warning", VBFeaturesResources.Disables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line))
            ElseIf context.IsPreProcessorDirectiveContext Then
                If context.TargetToken.IsKind(SyntaxKind.EnableKeyword) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Warning", VBFeaturesResources.Enables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line))
                ElseIf context.TargetToken.IsKind(SyntaxKind.DisableKeyword) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Warning", VBFeaturesResources.Disables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
