' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    Friend Class MockCompletionProvider
        Inherits CompletionListProvider

        Private ReadOnly _span As TextSpan

        Public Sub New(span As TextSpan)
            Me._span = span
        End Sub

        Public Overrides Function ProduceCompletionListAsync(context As CompletionListContext) As Task
            Dim item = New CompletionItem(Me, "DisplayText", _span)
            context.AddCompletionItem(item)

            Return SpecializedTasks.EmptyTask
        End Function

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean
            Return False
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return True
        End Function

        Public Overrides Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean
            Return False
        End Function

        Public Overrides Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange
            Return New TextChange(selectedItem.FilterSpan, "InsertionText")
        End Function

        Public Overrides Function IsFilterCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean
            Return False
        End Function
    End Class
End Namespace
