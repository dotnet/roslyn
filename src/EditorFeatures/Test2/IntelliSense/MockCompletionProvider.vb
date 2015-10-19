' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class MockCompletionProvider
        Inherits CompletionListProvider

        Private ReadOnly _getItems As Func(Of Document, Integer, CancellationToken, IEnumerable(Of CompletionItem))
        Private ReadOnly _isTriggerCharacter As Func(Of SourceText, Integer, Boolean)

        Public Sub New(Optional getItems As Func(Of Document, Integer, CancellationToken, IEnumerable(Of CompletionItem)) = Nothing,
                       Optional isTriggerCharacter As Func(Of SourceText, Integer, Boolean) = Nothing)
            Me._getItems = getItems
            Me._isTriggerCharacter = isTriggerCharacter
        End Sub

        Public Overrides Function ProduceCompletionListAsync(context As CompletionListContext) As Task
            If _getItems Is Nothing Then
                Return SpecializedTasks.EmptyTask
            End If

            Dim items = _getItems(context.Document, context.Position, context.CancellationToken)

            If items Is Nothing Then
                Return SpecializedTasks.EmptyTask
            End If

            For Each item In items
                context.AddItem(item)
            Next

            Return SpecializedTasks.EmptyTask
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return If(_isTriggerCharacter Is Nothing, Nothing, _isTriggerCharacter(text, characterPosition))
        End Function

    End Class
End Namespace
