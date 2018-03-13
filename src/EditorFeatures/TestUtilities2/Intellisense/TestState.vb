' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.BraceCompletion
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Roslyn.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Partial Friend Class TestState
        Inherits AbstractCommandHandlerTestState
        Implements IIntelliSenseTestState

        Private _currentCompletionPresenterSession As TestCompletionPresenterSession

        Friend ReadOnly AsyncCompletionService As IAsyncCompletionService
        Friend ReadOnly SignatureHelpCommandHandler As SignatureHelpCommandHandler
        Friend ReadOnly FormatCommandHandler As FormatCommandHandler
        Friend ReadOnly CompletionCommandHandler As CompletionCommandHandler
        Friend ReadOnly IntelliSenseCommandHandler As IntelliSenseCommandHandler

        Friend Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession Implements IIntelliSenseTestState.CurrentSignatureHelpPresenterSession
        Friend Property CurrentCompletionPresenterSession As TestCompletionPresenterSession Implements IIntelliSenseTestState.CurrentCompletionPresenterSession
            Get
                Return _currentCompletionPresenterSession
            End Get
            Set(value As TestCompletionPresenterSession)
                _currentCompletionPresenterSession = value
            End Set
        End Property

        Private Sub New(workspaceElement As XElement,
                        extraCompletionProviders As IEnumerable(Of Lazy(Of CompletionProvider, OrderableLanguageAndRoleMetadata)),
                        extraSignatureHelpProviders As IEnumerable(Of Lazy(Of ISignatureHelpProvider, OrderableLanguageMetadata)),
                        Optional extraExportedTypes As List(Of Type) = Nothing,
                        Optional includeFormatCommandHandler As Boolean = False,
                        Optional workspaceKind As String = Nothing)
            MyBase.New(workspaceElement, MinimalTestExportProvider.CreateTypeCatalog(If(extraExportedTypes, New List(Of Type))), workspaceKind:=workspaceKind)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            If extraCompletionProviders IsNot Nothing Then
                Dim completionService = DirectCast(languageServices.GetService(Of CompletionService), CompletionServiceWithProviders)
                completionService.SetTestProviders(extraCompletionProviders.Select(Function(lz) lz.Value).ToList())
            End If

            Me.AsyncCompletionService = New AsyncCompletionService(
                GetService(Of IEditorOperationsFactoryService)(),
                UndoHistoryRegistry,
                GetService(Of IInlineRenameService)(),
                GetExportedValue(Of IAsynchronousOperationListenerProvider),
                {New Lazy(Of IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession), OrderableMetadata)(Function() New TestCompletionPresenter(Me), New OrderableMetadata("Presenter"))},
                GetExports(Of IBraceCompletionSessionProvider, BraceCompletionMetadata)())

            Me.CompletionCommandHandler = New CompletionCommandHandler(Me.AsyncCompletionService)

            Me.SignatureHelpCommandHandler = New SignatureHelpCommandHandler(
                New TestSignatureHelpPresenter(Me),
                GetExports(Of ISignatureHelpProvider, OrderableLanguageMetadata)().Concat(extraSignatureHelpProviders),
                GetExportedValue(Of IAsynchronousOperationListenerProvider)())

            Me.IntelliSenseCommandHandler = New IntelliSenseCommandHandler(CompletionCommandHandler, SignatureHelpCommandHandler, Nothing)

            Me.FormatCommandHandler = If(includeFormatCommandHandler,
                New FormatCommandHandler(
                    GetService(Of ITextUndoHistoryRegistry),
                    GetService(Of IEditorOperationsFactoryService)),
                Nothing)
        End Sub

        Public Shared Function CreateVisualBasicTestState(
                documentElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing,
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
                CreateLazyProviders(extraSignatureHelpProviders, LanguageNames.VisualBasic),
                extraExportedTypes)
        End Function

        Public Shared Function CreateCSharpTestState(
                documentElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing,
                Optional includeFormatCommandHandler As Boolean = False) As TestState
            Return New TestState(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <%= documentElement.Value %>
                        </Document>
                    </Project>
                </Workspace>,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.CSharp, roles:=Nothing),
                CreateLazyProviders(extraSignatureHelpProviders, LanguageNames.CSharp),
                extraExportedTypes,
                includeFormatCommandHandler)
        End Function

        Public Shared Function CreateTestStateFromWorkspace(
                workspaceElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing,
                Optional workspaceKind As String = Nothing) As TestState
            Return New TestState(
                workspaceElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                CreateLazyProviders(extraSignatureHelpProviders, LanguageNames.VisualBasic),
                extraExportedTypes,
                workspaceKind:=workspaceKind)
        End Function

#Region "IntelliSense Operations"

        Public Overloads Sub SendEscape()
            MyBase.SendEscape(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendDownKey()
            MyBase.SendDownKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendUpKey()
            MyBase.SendUpKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

#End Region

#Region "Completion Operations"
        Public Overloads Sub SendTab()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of TabKeyCommandArgs))
            MyBase.SendTab(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of ReturnKeyCommandArgs))
            MyBase.SendReturn(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertNewLine())
        End Sub

        Public Overloads Sub SendPageUp()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendCut()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CutCommandArgs))
            MyBase.SendCut(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendPaste()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PasteCommandArgs))
            MyBase.SendPaste(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendInvokeCompletionList()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendCommitUniqueCompletionListItem()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSelectCompletionItem(displayText As String)
            Dim item = CurrentCompletionPresenterSession.CompletionItems.FirstOrDefault(Function(i) i.DisplayText = displayText)
            Assert.NotNull(item)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overloads Sub SendSelectCompletionItemThroughPresenterSession(item As CodeAnalysis.Completion.CompletionItem)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overloads Sub SendInsertSnippetCommand()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InsertSnippetCommandArgs))
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSurroundWithCommand()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SurroundWithCommandArgs))
            MyBase.SendSurroundWithCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSave()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SaveCommandArgs))
            MyBase.SendSave(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSelectAll()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SelectAllCommandArgs))
            MyBase.SendSelectAll(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Async Function AssertNoCompletionSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If
            Assert.Null(Me.CurrentCompletionPresenterSession)
        End Function

        Public Async Function AssertCompletionSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentCompletionPresenterSession)
        End Function

        Public Async Function AssertLineTextAroundCaret(expectedTextBeforeCaret As String, expectedTextAfterCaret As String) As Task
            Await WaitForAsynchronousOperationsAsync()

            Dim actual = GetLineTextAroundCaretPosition()

            Assert.Equal(expectedTextBeforeCaret, actual.TextBeforeCaret)
            Assert.Equal(expectedTextAfterCaret, actual.TextAfterCaret)
        End Function

        Public Function CompletionItemsContainsAll(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentCompletionPresenterSession.CompletionItems.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Function CompletionItemsContainsAny(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.Any(Function(v) CurrentCompletionPresenterSession.CompletionItems.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Sub AssertItemsInOrder(expectedOrder As String())
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
                               Optional shouldFormatOnCommit As Boolean? = Nothing) As Task
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

        Public Overloads Sub SendTypeChars(typeChars As String)
            MyBase.SendTypeChars(typeChars, Sub(a, n, c) ExecuteTypeCharCommand(a, n, c))
        End Sub

        Public Overloads Sub SendBackspace()
            Dim compHandler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of BackspaceKeyCommandArgs))
            MyBase.SendBackspace(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendBackspace)
        End Sub

        Public Overloads Sub SendDelete()
            Dim compHandler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of DeleteKeyCommandArgs))
            MyBase.SendDelete(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDelete)
        End Sub

        Public Sub SendTypeCharsToSpecificViewAndBuffer(typeChars As String, view As IWpfTextView, buffer As ITextBuffer)
            For Each ch In typeChars
                Dim localCh = ch
                ExecuteTypeCharCommand(New TypeCharCommandArgs(view, buffer, localCh), Sub() EditorOperations.InsertText(localCh.ToString()), TestCommandExecutionContext.Create())
            Next
        End Sub
#End Region

#Region "Signature Help Operations"

        Public Overloads Sub SendInvokeSignatureHelp()
            Dim handler = DirectCast(SignatureHelpCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeSignatureHelpCommandArgs))
            MyBase.SendInvokeSignatureHelp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Async Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If

            Assert.Null(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Async Function AssertSignatureHelpSession() As Task
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

        Public Function SignatureHelpItemsContainsAll(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                       Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v))
        End Function

        Public Function SignatureHelpItemsContainsAny(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.Any(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                       Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v))
        End Function

        Public Async Function AssertSelectedSignatureHelpItem(Optional displayText As String = Nothing,
                               Optional documentation As String = Nothing,
                               Optional selectedParameter As String = Nothing) As Task
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
