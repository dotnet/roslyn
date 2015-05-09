' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    Friend Class MockCompletionProvider
        Implements ICompletionProvider

        Private ReadOnly _span As TextSpan

        Public Sub New(span As TextSpan)
            Me._span = span
        End Sub

        Public Function GetGroupAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, Optional cancellationToken As CancellationToken = Nothing) As Task(Of CompletionItemGroup) Implements ICompletionProvider.GetGroupAsync
            Dim item = New CompletionItem(Me, "DisplayText", _span)
            Return Task.FromResult(New CompletionItemGroup(SpecializedCollections.SingletonEnumerable(item)))
        End Function

        Public Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean Implements ICompletionProvider.IsCommitCharacter
            Return False
        End Function

        Public Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean Implements ICompletionProvider.IsTriggerCharacter
            Return True
        End Function

        Public Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean Implements ICompletionProvider.SendEnterThroughToEditor
            Return False
        End Function

        Public Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange Implements ICompletionProvider.GetTextChange
            Return New TextChange(selectedItem.FilterSpan, "InsertionText")
        End Function

        Public Function IsFilterCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean Implements ICompletionProvider.IsFilterCharacter
            Return False
        End Function
    End Class
End Namespace
