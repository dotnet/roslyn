' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend MustInherit Class MockCompletionProvider
        Inherits CommonCompletionProvider

        Private ReadOnly _getItems As Func(Of Document, Integer, CancellationToken, IEnumerable(Of CompletionItem))
        Private ReadOnly _isTriggerCharacter As Func(Of SourceText, Integer, Boolean)

        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(Optional getItems As Func(Of Document, Integer, CancellationToken, IEnumerable(Of CompletionItem)) = Nothing,
                       Optional isTriggerCharacter As Func(Of SourceText, Integer, Boolean) = Nothing)
            Me._getItems = getItems
            Me._isTriggerCharacter = isTriggerCharacter
        End Sub

        Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
            If _getItems Is Nothing Then
                Return Task.CompletedTask
            End If

            Dim items = _getItems(context.Document, context.Position, context.CancellationToken)

            If items Is Nothing Then
                Return Task.CompletedTask
            End If

            For Each item In items
                context.AddItem(item)
            Next

            Return Task.CompletedTask
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return If(_isTriggerCharacter Is Nothing, Nothing, _isTriggerCharacter(text, characterPosition))
        End Function
    End Class
End Namespace
