' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Interface ITestState
        Inherits IDisposable

#Region "Base class methods and properties implemented in the interface"

        ReadOnly Property Workspace As TestWorkspace

        ReadOnly Property SubjectBuffer As ITextBuffer

        ReadOnly Property TextView As ITextView

        Function GetCompletionCommandHandler() As CompletionCommandHandler

        Function GetExportedValue(Of T)() As T

        Function GetService(Of T)() As T

        Function GetDocumentText() As String

        Sub SendDeleteWordToLeft()

        Function GetSelectedItem() As CompletionItem

        Function GetSelectedItemOpt() As CompletionItem

        Function GetCompletionItems() As IList(Of CompletionItem)

        Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)

        Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)

        Function GetSuggestionModeItem() As CompletionItem

        Function IsSoftSelected() As Boolean

        Function GetSignatureHelpItems() As IList(Of SignatureHelpItem)

        Sub SendMoveToPreviousCharacter(Optional extendSelection As Boolean = False)

        Sub AssertMatchesTextStartingAtLine(line As Integer, text As String)

        Function GetLineFromCurrentCaretPosition() As ITextSnapshotLine

        Function GetCaretPoint() As CaretPosition

#End Region

#Region "IntelliSense Operations"
        Sub SendEscape()

        Sub SendDownKey()

        Sub SendUpKey()

        Sub SendLeftKey()

        Sub SendRightKey()

        Sub SendUndo()

#End Region

#Region "Completion Operations"

        Sub SendTypeChars(typeChars As String)

        Sub SendTab()

        Sub SendReturn()

        Sub SendPageUp()

        Sub SendCut()

        Sub SendPaste()

        Sub SendInvokeCompletionList()

        Sub SendCommitUniqueCompletionListItem()

        Sub SendSelectCompletionItem(displayText As String)

        Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)

        Sub SendInsertSnippetCommand()

        Sub SendSurroundWithCommand()

        Sub SendSave()

        Sub SendSelectAll()

        Function AssertNoCompletionSession(Optional block As Boolean = True) As Task

        Function AssertCompletionSession() As Task

        Function AssertLineTextAroundCaret(expectedTextBeforeCaret As String, expectedTextAfterCaret As String) As Task

        Function CompletionItemsContainsAll(displayText As String()) As Boolean

        Function CompletionItemsContainsAny(displayText As String()) As Boolean

        Sub AssertItemsInOrder(expectedOrder As String())

        Function GetLineTextFromCaretPosition() As String
#End Region

#Region "Signature Help and Completion Operations"

        Sub SendBackspace()


        Sub SendDelete()

        Sub SendTypeCharsToSpecificViewAndBuffer(typeChars As String, view As IWpfTextView, buffer As ITextBuffer)
#End Region

#Region "Signature Help Operations"

        Sub SendInvokeSignatureHelp()

        Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task

        Function SignatureHelpItemsContainsAll(displayText As String()) As Boolean

        Function SignatureHelpItemsContainsAny(displayText As String()) As Boolean

        Function AssertSelectedSignatureHelpItem(Optional displayText As String = Nothing,
                               Optional documentation As String = Nothing,
                               Optional selectedParameter As String = Nothing) As Task

        Function AssertSignatureHelpSession() As Task

        Function WaitForAsynchronousOperationsAsync() As Task

        Function AssertSelectedCompletionItem(
                               Optional displayText As String = Nothing,
                               Optional description As String = Nothing,
                               Optional isSoftSelected As Boolean? = Nothing,
                               Optional isHardSelected As Boolean? = Nothing,
                               Optional displayTextSuffix As String = Nothing,
                               Optional shouldFormatOnCommit As Boolean? = Nothing) As Task
#End Region
    End Interface
End Namespace
