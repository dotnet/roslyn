' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
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

        Friend Overrides Async Function GetNavigationLocationAsync(
                document As Document,
                item As NavigationBarItem,
                symbolItem As SymbolItem,
                textVersion As ITextVersion,
                cancellationToken As CancellationToken) As Task(Of (documentId As DocumentId, position As Integer, virtualSpace As Integer))

            Dim navigationLocation = Await MyBase.GetNavigationLocationAsync(
                document, item, symbolItem, textVersion, cancellationToken).ConfigureAwait(False)

            Dim destinationDocument = document.Project.Solution.GetDocument(navigationLocation.documentId)

            Dim root = Await destinationDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            ' If the symbol is a method symbol, we'll figure out the right location which may be in virtual space
            Dim methodBlock = root.FindToken(navigationLocation.position).GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock IsNot Nothing Then
                Dim text = Await destinationDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(False)
                Dim navPoint = NavigationPointHelpers.GetNavigationPoint(text, indentSize:=4, methodBlock)
                Return (navigationLocation.documentId, navPoint.Position, navPoint.VirtualSpaces)
            End If

            Return navigationLocation
        End Function

        Protected Overrides Async Function TryNavigateToItemAsync(
                document As Document, item As WrappedNavigationBarItem, textView As ITextView, textVersion As ITextVersion, cancellationToken As CancellationToken) As Task(Of Boolean)
            Dim underlying = item.UnderlyingItem

            Dim generateCodeItem = TryCast(underlying, AbstractGenerateCodeItem)
            Dim symbolItem = TryCast(underlying, SymbolItem)
            If generateCodeItem IsNot Nothing Then
                Await GenerateCodeForItemAsync(document, generateCodeItem, textView, cancellationToken).ConfigureAwait(False)
                Return True
            ElseIf symbolItem IsNot Nothing Then
                Await NavigateToSymbolItemAsync(document, item, symbolItem, textVersion, cancellationToken).ConfigureAwait(False)
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
