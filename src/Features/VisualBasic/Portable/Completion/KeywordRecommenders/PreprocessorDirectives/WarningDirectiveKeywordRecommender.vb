' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#Disable" preprocessor directive
    ''' </summary>
    Friend Class WarningDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreprocessorStartContext AndAlso Not context.SyntaxTree.IsEnumMemberNameContext(context) Then
                Return {New RecommendedKeyword("#Enable Warning", VBFeaturesResources.Enables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line),
                        New RecommendedKeyword("#Disable Warning", VBFeaturesResources.Disables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line)}
            ElseIf context.IsPreProcessorDirectiveContext Then
                If context.TargetToken.IsKind(SyntaxKind.EnableKeyword) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Warning", VBFeaturesResources.Enables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line))
                ElseIf context.TargetToken.IsKind(SyntaxKind.DisableKeyword) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Warning", VBFeaturesResources.Disables_reporting_of_specified_warnings_in_the_portion_of_the_source_file_below_the_current_line))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
