' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.BraceCompletion
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Roslyn.Utilities
Imports CompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem
Imports VSCommanding = Microsoft.VisualStudio.Commanding
Imports Microsoft.CodeAnalysis.CSharp

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Partial Friend Class TestState
        Inherits AbstractCommandHandlerTestState
        Implements ITestState

        Friend ReadOnly AsyncCompletionService As IAsyncCompletionService
        Friend ReadOnly SignatureHelpCommandHandler As SignatureHelpCommandHandler
        Friend ReadOnly FormatCommandHandler As FormatCommandHandler
        Friend ReadOnly CompletionCommandHandler As CompletionCommandHandler
        Friend ReadOnly IntelliSenseCommandHandler As IntelliSenseCommandHandler
        Private ReadOnly SessionTestState As IIntelliSenseTestState

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        Friend ReadOnly Property CurrentCompletionPresenterSession As TestCompletionPresenterSession
            Get
                Return SessionTestState.CurrentCompletionPresenterSession
            End Get
        End Property

        Private Sub New(workspaceElement As XElement,
                        extraCompletionProviders As IEnumerable(Of Lazy(Of CompletionProvider, OrderableLanguageAndRoleMetadata)),
                        Optional excludedTypes As List(Of Type) = Nothing,
                        Optional extraExportedTypes As List(Of Type) = Nothing,
                        Optional includeFormatCommandHandler As Boolean = False,
                        Optional workspaceKind As String = Nothing)
            MyBase.New(workspaceElement, CombineExcludedTypes(excludedTypes, includeFormatCommandHandler), ExportProviderCache.CreateTypeCatalog(CombineExtraTypes(If(extraExportedTypes, New List(Of Type)))), workspaceKind:=workspaceKind)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            If extraCompletionProviders IsNot Nothing Then
                Dim completionService = DirectCast(languageServices.GetService(Of CompletionService), CompletionServiceWithProviders)
                If completionService IsNot Nothing Then
                    completionService.SetTestProviders(extraCompletionProviders.Select(Function(lz) lz.Value).ToList())
                End If
            End If

            Me.SessionTestState = GetExportedValue(Of IIntelliSenseTestState)()

            Me.AsyncCompletionService = GetExportedValue(Of IAsyncCompletionService)()

            Me.CompletionCommandHandler = GetExportedValue(Of CompletionCommandHandler)()

            Me.SignatureHelpCommandHandler = GetExportedValue(Of SignatureHelpCommandHandler)()

            Me.IntelliSenseCommandHandler = GetExportedValue(Of IntelliSenseCommandHandler)()

            Me.FormatCommandHandler = If(includeFormatCommandHandler,
                GetExportedValue(Of FormatCommandHandler)(),
                Nothing)
        End Sub

        Private Shared Function CombineExcludedTypes(excludedTypes As IList(Of Type), includeFormatCommandHandler As Boolean) As IList(Of Type)
            Dim result = New List(Of Type) From {
                GetType(IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)),
                GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            }

            If Not includeFormatCommandHandler Then
                result.Add(GetType(FormatCommandHandler))
            End If

            If excludedTypes IsNot Nothing Then
                result.AddRange(excludedTypes)
            End If

            Return result
        End Function

        Private Shared Function CombineExtraTypes(extraExportedTypes As IList(Of Type)) As IList(Of Type)
            Dim result = New List(Of Type) From {
                GetType(TestCompletionPresenter),
                GetType(TestSignatureHelpPresenter),
                GetType(IntelliSenseTestState)
            }

            If extraExportedTypes IsNot Nothing Then
                result.AddRange(extraExportedTypes)
            End If

            Return result
        End Function

        Friend Shared Function CreateVisualBasicTestState(
                documentElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing) As TestState
            Return New TestState(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            <%= documentElement.Value %>
                        </Document>
                    </Project>
                </Workspace>,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                excludedTypes:=Nothing,
                extraExportedTypes)
        End Function

        Friend Shared Function CreateCSharpTestState(
                documentElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional excludedTypes As List(Of Type) = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing,
                Optional includeFormatCommandHandler As Boolean = False,
                Optional languageVersion As LanguageVersion = LanguageVersion.Default) As TestState
            Return New TestState(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion=<%= DirectCast(languageVersion, Int32) %>>
                        <Document>
                            <%= documentElement.Value %>
                        </Document>
                    </Project>
                </Workspace>,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.CSharp, roles:=Nothing),
                excludedTypes,
                extraExportedTypes,
                includeFormatCommandHandler)
        End Function

        Friend Shared Function CreateTestStateFromWorkspace(
                workspaceElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing,
                Optional workspaceKind As String = Nothing) As TestState
            Return New TestState(
                workspaceElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                excludedTypes:=Nothing,
                extraExportedTypes,
                workspaceKind:=workspaceKind)
        End Function

#Region "Base class methods and properties implemented in the interface"
        Public Shadows ReadOnly Property Workspace As TestWorkspace Implements ITestState.Workspace
            Get
                Return MyBase.Workspace
            End Get
        End Property

        Public Shadows ReadOnly Property SubjectBuffer As ITextBuffer Implements ITestState.SubjectBuffer
            Get
                Return MyBase.SubjectBuffer
            End Get
        End Property

        Public Shadows ReadOnly Property TextView As ITextView Implements ITestState.TextView
            Get
                Return MyBase.TextView
            End Get
        End Property

        Public Shadows Function GetExportedValue(Of T)() As T Implements ITestState.GetExportedValue
            Return MyBase.GetExportedValue(Of T)()
        End Function

        Public Shadows Function GetService(Of T)() As T Implements ITestState.GetService
            Return MyBase.GetService(Of T)()
        End Function

        Public Shadows Function GetDocumentText() As String Implements ITestState.GetDocumentText
            Return MyBase.GetDocumentText()
        End Function

        Public Shadows Async Function WaitForAsynchronousOperationsAsync() As Task Implements ITestState.WaitForAsynchronousOperationsAsync
            Await MyBase.WaitForAsynchronousOperationsAsync()
        End Function

        Public Shadows Sub SendMoveToPreviousCharacter(Optional extendSelection As Boolean = False) Implements ITestState.SendMoveToPreviousCharacter
            MyBase.SendMoveToPreviousCharacter(extendSelection)
        End Sub

        Public Shadows Sub SendDeleteWordToLeft() Implements ITestState.SendDeleteWordToLeft
            MyBase.SendDeleteWordToLeft()
        End Sub

        Public Function GetSelectedItem() As CompletionItem Implements ITestState.GetSelectedItem
            Return CurrentCompletionPresenterSession.SelectedItem
        End Function

        Public Function GetSelectedItemOpt() As CompletionItem Implements ITestState.GetSelectedItemOpt
            Return CurrentCompletionPresenterSession?.SelectedItem
        End Function

        Public Function GetCompletionItems() As IList(Of CompletionItem) Implements ITestState.GetCompletionItems
            Return CurrentCompletionPresenterSession.CompletionItems
        End Function

        Public Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs) Implements ITestState.RaiseFiltersChanged
            CurrentCompletionPresenterSession.RaiseFiltersChanged(args)
        End Sub

        Public Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter) Implements ITestState.GetCompletionItemFilters
            Return CurrentCompletionPresenterSession.CompletionItemFilters
        End Function

        Public Function GetSuggestionModeItem() As CompletionItem Implements ITestState.GetSuggestionModeItem
            Return CurrentCompletionPresenterSession.SuggestionModeItem
        End Function

        Public Function IsSoftSelected() As Boolean Implements ITestState.IsSoftSelected
            Return CurrentCompletionPresenterSession.IsSoftSelected
        End Function

        Public Function GetSignatureHelpItems() As IList(Of SignatureHelpItem) Implements ITestState.GetSignatureHelpItems
            Return CurrentSignatureHelpPresenterSession.SignatureHelpItems
        End Function

        Public Shadows Sub AssertMatchesTextStartingAtLine(line As Integer, text As String) Implements ITestState.AssertMatchesTextStartingAtLine
            MyBase.AssertMatchesTextStartingAtLine(line, text)
        End Sub

        Public Shadows Function GetLineFromCurrentCaretPosition() As ITextSnapshotLine Implements ITestState.GetLineFromCurrentCaretPosition
            Return MyBase.GetLineFromCurrentCaretPosition()
        End Function

        Public Shadows Function GetCaretPoint() As CaretPosition Implements ITestState.GetCaretPoint
            Return MyBase.GetCaretPoint()
        End Function

        Public Shadows Function GetLineTextFromCaretPosition() As String Implements ITestState.GetLineTextFromCaretPosition
            Return MyBase.GetLineTextFromCaretPosition()
        End Function

        Public Function GetCompletionCommandHandler() As CompletionCommandHandler Implements ITestState.GetCompletionCommandHandler
            Return CompletionCommandHandler
        End Function

#End Region

#Region "IntelliSense Operations"

        Public Overloads Sub SendEscape() Implements ITestState.SendEscape
            MyBase.SendEscape(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendDownKey() Implements ITestState.SendDownKey
            MyBase.SendDownKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendUpKey() Implements ITestState.SendUpKey
            MyBase.SendUpKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Shadows Sub SendLeftKey() Implements ITestState.SendLeftKey
            MyBase.SendLeftKey()
        End Sub

        Public Shadows Sub SendRightKey() Implements ITestState.SendRightKey
            MyBase.SendRightKey()
        End Sub

        Public Shadows Sub SendUndo() Implements ITestState.SendUndo
            MyBase.SendUndo()
        End Sub

#End Region

#Region "Completion Operations"
        Public Overloads Sub SendTab() Implements ITestState.SendTab
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of TabKeyCommandArgs))
            MyBase.SendTab(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn() Implements ITestState.SendReturn
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of ReturnKeyCommandArgs))
            MyBase.SendReturn(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertNewLine())
        End Sub

        Public Overloads Sub SendPageUp() Implements ITestState.SendPageUp
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendCut() Implements ITestState.SendCut
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CutCommandArgs))
            MyBase.SendCut(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendPaste() Implements ITestState.SendPaste
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PasteCommandArgs))
            MyBase.SendPaste(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendInvokeCompletionList() Implements ITestState.SendInvokeCompletionList
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendCommitUniqueCompletionListItem() Implements ITestState.SendCommitUniqueCompletionListItem
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSelectCompletionItem(displayText As String) Implements ITestState.SendSelectCompletionItem
            Dim item = CurrentCompletionPresenterSession.CompletionItems.FirstOrDefault(Function(i) i.DisplayText = displayText)
            Assert.NotNull(item)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overloads Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem) Implements ITestState.SendSelectCompletionItemThroughPresenterSession
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overloads Sub SendInsertSnippetCommand() Implements ITestState.SendInsertSnippetCommand
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InsertSnippetCommandArgs))
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSurroundWithCommand() Implements ITestState.SendSurroundWithCommand
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SurroundWithCommandArgs))
            MyBase.SendSurroundWithCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSave() Implements ITestState.SendSave
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SaveCommandArgs))
            MyBase.SendSave(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSelectAll() Implements ITestState.SendSelectAll
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SelectAllCommandArgs))
            MyBase.SendSelectAll(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Async Function AssertNoCompletionSession(Optional block As Boolean = True) As Task Implements ITestState.AssertNoCompletionSession
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If
            Assert.Null(Me.CurrentCompletionPresenterSession)
        End Function

        Public Async Function AssertCompletionSession() As Task Implements ITestState.AssertCompletionSession
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentCompletionPresenterSession)
        End Function

        Public Async Function AssertLineTextAroundCaret(expectedTextBeforeCaret As String, expectedTextAfterCaret As String) As Task Implements ITestState.AssertLineTextAroundCaret
            Await WaitForAsynchronousOperationsAsync()

            Dim actual = GetLineTextAroundCaretPosition()

            Assert.Equal(expectedTextBeforeCaret, actual.TextBeforeCaret)
            Assert.Equal(expectedTextAfterCaret, actual.TextAfterCaret)
        End Function

        Public Function CompletionItemsContainsAll(displayText As String()) As Boolean Implements ITestState.CompletionItemsContainsAll
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentCompletionPresenterSession.CompletionItems.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Function CompletionItemsContainsAny(displayText As String()) As Boolean Implements ITestState.CompletionItemsContainsAny
            AssertNoAsynchronousOperationsRunning()
            Return displayText.Any(Function(v) CurrentCompletionPresenterSession.CompletionItems.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Sub AssertItemsInOrder(expectedOrder As String()) Implements ITestState.AssertItemsInOrder
            AssertNoAsynchronousOperationsRunning()
            Dim items = CurrentCompletionPresenterSession.CompletionItems
            Assert.Equal(expectedOrder.Count, items.Count)
            For i = 0 To expectedOrder.Count - 1
                Assert.Equal(expectedOrder(i), items(i).DisplayText)
            Next
        End Sub

        Public Async Function AssertSelectedCompletionItem(
                               Optional displayText As String = Nothing,
                               Optional description As String = Nothing,
                               Optional isSoftSelected As Boolean? = Nothing,
                               Optional isHardSelected As Boolean? = Nothing,
                               Optional displayTextSuffix As String = Nothing,
                               Optional shouldFormatOnCommit As Boolean? = Nothing) As Task Implements ITestState.AssertSelectedCompletionItem

            Await WaitForAsynchronousOperationsAsync()
            If isSoftSelected.HasValue Then
                Assert.True(isSoftSelected.Value = Me.CurrentCompletionPresenterSession.IsSoftSelected, "Current completion is not soft-selected.")
            End If

            If isHardSelected.HasValue Then
                Assert.True(isHardSelected.Value = Not Me.CurrentCompletionPresenterSession.IsSoftSelected, "Current completion is not hard-selected.")
            End If

            If displayText IsNot Nothing Then
                Assert.Equal(displayText, Me.CurrentCompletionPresenterSession.SelectedItem.DisplayText)
            End If

            If displayTextSuffix IsNot Nothing Then
                Assert.Equal(displayTextSuffix, Me.CurrentCompletionPresenterSession.SelectedItem.DisplayTextSuffix)
            End If

            If shouldFormatOnCommit.HasValue Then
                Assert.Equal(shouldFormatOnCommit.Value, Me.CurrentCompletionPresenterSession.SelectedItem.Rules.FormatOnCommit)
            End If

#If False Then
            If insertionText IsNot Nothing Then
                Assert.Equal(insertionText, Me.CurrentCompletionPresenterSession.SelectedItem.TextChange.NewText)
            End If
#End If

            If description IsNot Nothing Then
                Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim itemDescription = Await service.GetDescriptionAsync(
                    document, Me.CurrentCompletionPresenterSession.SelectedItem)
                Assert.Equal(description, itemDescription.Text)
            End If
        End Function

#End Region

#Region "Signature Help and Completion Operations"

        Private Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext)
            Dim sigHelpHandler = DirectCast(SignatureHelpCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim formatHandler = DirectCast(FormatCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim compHandler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))

            If formatHandler Is Nothing Then
                sigHelpHandler.ExecuteCommand(
                    args, Sub() compHandler.ExecuteCommand(
                                    args, finalHandler, context), context)
            Else
                formatHandler.ExecuteCommand(
                    args, Sub() sigHelpHandler.ExecuteCommand(
                                    args, Sub() compHandler.ExecuteCommand(
                                                    args, finalHandler, context), context), context)
            End If
        End Sub

        Public Overloads Sub SendTypeChars(typeChars As String) Implements ITestState.SendTypeChars
            MyBase.SendTypeChars(typeChars, Sub(a, n, c) ExecuteTypeCharCommand(a, n, c))
        End Sub

        Public Overloads Sub SendBackspace() Implements ITestState.SendBackspace
            Dim compHandler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of BackspaceKeyCommandArgs))
            MyBase.SendBackspace(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendBackspace)
        End Sub

        Public Overloads Sub SendDelete() Implements ITestState.SendDelete
            Dim compHandler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of DeleteKeyCommandArgs))
            MyBase.SendDelete(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDelete)
        End Sub

        Public Sub SendTypeCharsToSpecificViewAndBuffer(typeChars As String, view As IWpfTextView, buffer As ITextBuffer) Implements ITestState.SendTypeCharsToSpecificViewAndBuffer
            For Each ch In typeChars
                Dim localCh = ch
                ExecuteTypeCharCommand(New TypeCharCommandArgs(view, buffer, localCh), Sub() EditorOperations.InsertText(localCh.ToString()), TestCommandExecutionContext.Create())
            Next
        End Sub
#End Region

#Region "Signature Help Operations"

        Public Overloads Sub SendInvokeSignatureHelp() Implements ITestState.SendInvokeSignatureHelp
            Dim handler = DirectCast(SignatureHelpCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeSignatureHelpCommandArgs))
            MyBase.SendInvokeSignatureHelp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Async Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task Implements ITestState.AssertNoSignatureHelpSession
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If

            Assert.Null(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Async Function AssertSignatureHelpSession() As Task Implements ITestState.AssertSignatureHelpSession
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Private Function GetDisplayText(item As SignatureHelpItem, selectedParameter As Integer) As String
            Dim suffix = If(selectedParameter < item.Parameters.Count,
                            GetDisplayText(item.Parameters(selectedParameter).SuffixDisplayParts),
                            String.Empty)
            Return String.Join(
                String.Empty,
                GetDisplayText(item.PrefixDisplayParts),
                String.Join(
                    GetDisplayText(item.SeparatorDisplayParts),
                    item.Parameters.Select(Function(p) GetDisplayText(p.DisplayParts))),
                GetDisplayText(item.SuffixDisplayParts),
                suffix)
        End Function

        Private Function GetDisplayText(parts As IEnumerable(Of TaggedText)) As String
            Return String.Join(String.Empty, parts.Select(Function(p) p.ToString()))
        End Function

        Public Function SignatureHelpItemsContainsAll(displayText As String()) As Boolean Implements ITestState.SignatureHelpItemsContainsAll
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                       Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v))
        End Function

        Public Function SignatureHelpItemsContainsAny(displayText As String()) As Boolean Implements ITestState.SignatureHelpItemsContainsAny
            AssertNoAsynchronousOperationsRunning()
            Return displayText.Any(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                       Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v))
        End Function

        Public Async Function AssertSelectedSignatureHelpItem(Optional displayText As String = Nothing,
                               Optional documentation As String = Nothing,
                               Optional selectedParameter As String = Nothing) As Task Implements ITestState.AssertSelectedSignatureHelpItem
            Await WaitForAsynchronousOperationsAsync()

            If displayText IsNot Nothing Then
                Assert.Equal(displayText, GetDisplayText(Me.CurrentSignatureHelpPresenterSession.SelectedItem, Me.CurrentSignatureHelpPresenterSession.SelectedParameter.Value))
            End If

            If documentation IsNot Nothing Then
                Assert.Equal(documentation, Me.CurrentSignatureHelpPresenterSession.SelectedItem.DocumentationFactory(CancellationToken.None).GetFullText())
            End If

            If selectedParameter IsNot Nothing Then
                Assert.Equal(selectedParameter, GetDisplayText(
                    Me.CurrentSignatureHelpPresenterSession.SelectedItem.Parameters(
                        Me.CurrentSignatureHelpPresenterSession.SelectedParameter.Value).DisplayParts))
            End If
        End Function


#End Region
    End Class
End Namespace
