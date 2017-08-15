' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling
    ''' <summary>
    ''' Recommends the "RaiseEvent" keyword.
    ''' </summary>
    Friend Class RaiseEventKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsSingleLineStatementContext OrElse context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("RaiseEvent", VBFeaturesResources.Triggers_an_event_declared_at_module_level_within_a_class_form_or_document_RaiseEvent_eventName_bracket_argumentList_bracket))
            ElseIf context.CanDeclareCustomEventAccessor(SyntaxKind.RaiseEventAccessorBlock) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("RaiseEvent", VBFeaturesResources.Specifies_the_statements_to_run_when_the_event_is_raised_by_the_RaiseEvent_statement_RaiseEvent_delegateSignature_End_RaiseEvent))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
