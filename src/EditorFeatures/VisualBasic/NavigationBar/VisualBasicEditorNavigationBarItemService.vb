' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.NavigationBar
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
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
        Public Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService, textUndoHistoryRegistry As ITextUndoHistoryRegistry)
            _editorOperationsFactoryService = editorOperationsFactoryService
            _textUndoHistoryRegistry = textUndoHistoryRegistry
        End Sub

        Public Overrides Function ShowItemGrayedIfNear(item As NavigationBarItem) As Boolean
            ' We won't show gray things that don't actually exist
            Return DirectCast(DirectCast(item, WrappedNavigationBarItem).UnderlyingItem, CodeAnalysis.NavigationBar.RoslynNavigationBarItem).Kind = CodeAnalysis.NavigationBar.RoslynNavigationBarItemKind.Symbol
        End Function

        Public Overrides Function GetSymbolItemNavigationPoint(document As Document, item As RoslynNavigationBarItem, cancellationToken As CancellationToken) As VirtualTreePoint?
            Contract.ThrowIfFalse(item.Kind = RoslynNavigationBarItemKind.Symbol)
            Dim compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            Dim symbols = item.NavigationSymbolId.Value.Resolve(compilation, cancellationToken:=cancellationToken)
            Dim symbol = symbols.Symbol

            If symbol Is Nothing Then
                symbol = symbols.CandidateSymbols(item.NavigationSymbolIndex.Value)
            End If

            Dim location As Location = GetSourceNavigationLocation(document, symbol, cancellationToken)
            If location Is Nothing Then
                Return Nothing
            End If

            ' If the symbol is a method symbol, we'll figure out the right location which may be in
            ' virtual space
            If symbol.Kind = SymbolKind.Method Then
                Dim methodBlock = location.FindToken(cancellationToken).GetAncestor(Of MethodBlockBaseSyntax)()

                If methodBlock IsNot Nothing Then
                    Return NavigationPointHelpers.GetNavigationPoint(location.SourceTree.GetText(cancellationToken), 4, methodBlock)
                End If
            End If

            Return New VirtualTreePoint(location.SourceTree, location.SourceTree.GetText(cancellationToken), location.SourceSpan.Start)
        End Function

        Private Shared Function GetSourceNavigationLocation(document As Document, symbol As ISymbol, cancellationToken As CancellationToken) As Location
            Dim sourceLocations = symbol.Locations.Where(Function(l) l.IsInSource)

            ' First figure out the location that we want to grab considering partial types
            Dim syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken)
            Dim location = sourceLocations.FirstOrDefault(Function(l) l.SourceTree.Equals(syntaxTree))

            If location Is Nothing Then
                location = sourceLocations.FirstOrDefault
            End If

            Return location
        End Function

        Protected Overrides Sub NavigateToItem(document As Document, item As WrappedNavigationBarItem, textView As ITextView, cancellationToken As CancellationToken)
            Dim underlying = item.UnderlyingItem

            If underlying.Kind = RoslynNavigationBarItemKind.GenerateDefaultConstructor OrElse
               underlying.Kind = RoslynNavigationBarItemKind.GenerateEventHandler OrElse
               underlying.Kind = RoslynNavigationBarItemKind.GenerateFinalizer OrElse
               underlying.Kind = RoslynNavigationBarItemKind.GenerateMethod Then

                GenerateCodeForItem(document, underlying, textView, cancellationToken)
            ElseIf item.UnderlyingItem.Kind = RoslynNavigationBarItemKind.Symbol Then
                NavigateToSymbolItem(document, underlying, cancellationToken)
            End If
        End Sub
    End Class
End Namespace
