' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling
    ''' <summary>
    ''' Recommends the "RaiseEvent" keyword.
    ''' </summary>
    Friend Class RaiseEventKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsStatementContext OrElse context.IsMultiLineStatementContext Then
                Return ImmutableArray.Create(New RecommendedKeyword("RaiseEvent", VBFeaturesResources.Triggers_an_event_declared_at_module_level_within_a_class_form_or_document_RaiseEvent_eventName_bracket_argumentList_bracket))
            ElseIf context.CanDeclareCustomEventAccessor(SyntaxKind.RaiseEventAccessorBlock) Then
                Return ImmutableArray.Create(New RecommendedKeyword("RaiseEvent", VBFeaturesResources.Specifies_the_statements_to_run_when_the_event_is_raised_by_the_RaiseEvent_statement_RaiseEvent_delegateSignature_End_RaiseEvent))
            Else
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If
        End Function
    End Class
End Namespace
