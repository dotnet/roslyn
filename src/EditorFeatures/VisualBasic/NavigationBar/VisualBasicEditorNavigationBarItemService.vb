' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    <ExportLanguageService(GetType(INavigationBarItemService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicEditorNavigationBarItemService
        Inherits AbstractEditorNavigationBarItemService

        Private Shared ReadOnly GeneratedSymbolAnnotation As SyntaxAnnotation = New SyntaxAnnotation()

        Private ReadOnly _editorOperationsFactoryService As IEditorOperationsFactoryService
        Private ReadOnly _textUndoHistoryRegistry As ITextUndoHistoryRegistry

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
                threadingContext As IThreadingContext,
                editorOperationsFactoryService As IEditorOperationsFactoryService,
                textUndoHistoryRegistry As ITextUndoHistoryRegistry)
            MyBase.New(threadingContext)
            _editorOperationsFactoryService = editorOperationsFactoryService
            _textUndoHistoryRegistry = textUndoHistoryRegistry
        End Sub

        Public Overrides Function ShowItemGrayedIfNear(item As NavigationBarItem) As Boolean
            ' We won't show gray things that don't actually exist
            Return TypeOf DirectCast(item, WrappedNavigationBarItem).UnderlyingItem Is SymbolItem
        End Function

        Protected Overrides Async Function TryNavigateToItemAsync(
                document As Document, item As WrappedNavigationBarItem, textView As ITextView, textSnapshot As ITextSnapshot, cancellationToken As CancellationToken) As Task(Of Boolean)
            Dim underlying = item.UnderlyingItem

            Dim generateCodeItem = TryCast(underlying, AbstractGenerateCodeItem)
            Dim symbolItem = TryCast(underlying, SymbolItem)
            If generateCodeItem IsNot Nothing Then
                Await GenerateCodeForItemAsync(document, generateCodeItem, textView, cancellationToken).ConfigureAwait(False)
                Return True
            ElseIf symbolItem IsNot Nothing Then
                Await NavigateToSymbolItemAsync(document, item, symbolItem, textSnapshot, cancellationToken).ConfigureAwait(False)
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
