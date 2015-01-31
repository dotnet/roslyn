' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.Composition.Hosting
Imports System.ComponentModel.Composition.Primitives
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.[Shared].TestHooks
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.Intellisense.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class MockCompletionProvider
        Implements ICompletionProvider

        Private ReadOnly _getItems As Func(Of Document, Integer, CancellationToken, IEnumerable(Of CompletionItem))
        Private ReadOnly _isTriggerCharacter As Func(Of SourceText, Integer, Boolean)

        Public Sub New(Optional getItems As Func(Of Document, Integer, CancellationToken, IEnumerable(Of CompletionItem)) = Nothing,
                       Optional isTriggerCharacter As Func(Of SourceText, Integer, Boolean) = Nothing)
            Me._getItems = getItems
            Me._isTriggerCharacter = isTriggerCharacter
        End Sub

        Public Function GetGroupAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, Optional cancellationToken As CancellationToken = Nothing) As Tasks.Task(Of CompletionItemGroup) Implements ICompletionProvider.GetGroupAsync
            If _getItems Is Nothing Then
                Return Task.FromResult(Of CompletionItemGroup)(Nothing)
            End If

            Dim items = _getItems(document, position, cancellationToken)

            If items Is Nothing Then
                Return Task.FromResult(Of CompletionItemGroup)(Nothing)
            End If

            Return Task.FromResult(New CompletionItemGroup(items))
        End Function

        Public Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean Implements ICompletionProvider.IsCommitCharacter
            Return False
        End Function

        Public Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean Implements ICompletionProvider.IsTriggerCharacter
            Return If(_isTriggerCharacter Is Nothing, Nothing, _isTriggerCharacter(text, characterPosition))
        End Function

        Public Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean Implements ICompletionProvider.SendEnterThroughToEditor
            Return False
        End Function

        Public Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange Implements ICompletionProvider.GetTextChange
            Return New TextChange(selectedItem.FilterSpan, selectedItem.DisplayText)
        End Function

        Public Function IsFilterCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean Implements ICompletionProvider.IsFilterCharacter
            Return False
        End Function
    End Class
End Namespace
