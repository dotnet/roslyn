' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend MustInherit Class TestStateBase
        Inherits AbstractCommandHandlerTestState

        Protected ReadOnly SessionTestState As IIntelliSenseTestState
        Private ReadOnly SignatureHelpBeforeCompletionCommandHandler As SignatureHelpBeforeCompletionCommandHandler
        Protected ReadOnly SignatureHelpAfterCompletionCommandHandler As SignatureHelpAfterCompletionCommandHandler
        Private ReadOnly FormatCommandHandler As FormatCommandHandler

        Private Shared s_lazyEntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts As Lazy(Of ComposableCatalog) =
            New Lazy(Of ComposableCatalog)(Function()
                                               Return TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.
                                               WithoutPartsOfTypes({
                                                                   GetType(IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)),
                                                                   GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)),
                                                                   GetType(FormatCommandHandler)}).
                                               WithParts({
                                                         GetType(TestCompletionPresenter),
                                                         GetType(TestSignatureHelpPresenter),
                                                         GetType(IntelliSenseTestState),
                                                         GetType(MockCompletionPresenterProvider)
                                                         })
                                           End Function)

        Private Shared ReadOnly Property EntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts As ComposableCatalog
            Get
                Return s_lazyEntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts.Value
            End Get
        End Property

        Private Shared s_lazyExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts As Lazy(Of IExportProviderFactory) =
            New Lazy(Of IExportProviderFactory)(Function()
                                                    Return ExportProviderCache.GetOrCreateExportProviderFactory(EntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts)
                                                End Function)

        Private Shared ReadOnly Property ExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts As IExportProviderFactory
            Get
                Return s_lazyExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts.Value
            End Get
        End Property

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        Public Sub New(workspaceElement As XElement,
                       extraCompletionProviders As CompletionProvider(),
                       excludedTypes As List(Of Type),
                       extraExportedTypes As List(Of Type),
                       includeFormatCommandHandler As Boolean,
                       workspaceKind As String)
            MyBase.New(workspaceElement, GetExportProvider(excludedTypes, extraExportedTypes, includeFormatCommandHandler), workspaceKind:=workspaceKind)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            Dim lazyExtraCompletionProviders = CreateLazyProviders(extraCompletionProviders, language, roles:=Nothing)
            If lazyExtraCompletionProviders IsNot Nothing Then
                Dim completionService = DirectCast(languageServices.GetService(Of CompletionService), CompletionServiceWithProviders)
                If completionService IsNot Nothing Then
                    completionService.SetTestProviders(lazyExtraCompletionProviders.Select(Function(lz) lz.Value).ToList())
                End If
            End If

            Me.SessionTestState = GetExportedValue(Of IIntelliSenseTestState)()

            Me.SignatureHelpBeforeCompletionCommandHandler = GetExportedValue(Of SignatureHelpBeforeCompletionCommandHandler)()

            Me.SignatureHelpAfterCompletionCommandHandler = GetExportedValue(Of SignatureHelpAfterCompletionCommandHandler)()

            Me.FormatCommandHandler = If(includeFormatCommandHandler, GetExportedValue(Of FormatCommandHandler)(), Nothing)
        End Sub

        Private Overloads Shared Function GetExportProvider(excludedTypes As List(Of Type),
                                                  extraExportedTypes As List(Of Type),
                                                  includeFormatCommandHandler As Boolean) As ExportProvider
            If (excludedTypes Is Nothing OrElse excludedTypes.Count = 0) AndAlso
               (extraExportedTypes Is Nothing OrElse extraExportedTypes.Count = 0) AndAlso
               Not includeFormatCommandHandler Then
                Return ExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts.CreateExportProvider()
            End If

            Dim combinedExcludedTypes = CombineExcludedTypes(excludedTypes, includeFormatCommandHandler)
            Dim extraParts = ExportProviderCache.CreateTypeCatalog(CombineExtraTypes(If(extraExportedTypes, New List(Of Type))))
            Return GetExportProvider(combinedExcludedTypes, extraParts)
        End Function

#Region "Editor Related Operations"

        Protected Overloads Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext, completionCommandHandler As VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim sigHelpHandler = DirectCast(SignatureHelpBeforeCompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim formatHandler = DirectCast(FormatCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))

            If formatHandler Is Nothing Then
                sigHelpHandler.ExecuteCommand(
                    args, Sub() completionCommandHandler.ExecuteCommand(
                                    args, finalHandler, context), context)
            Else
                formatHandler.ExecuteCommand(
                    args, Sub() sigHelpHandler.ExecuteCommand(
                                    args, Sub() completionCommandHandler.ExecuteCommand(
                                                    args, finalHandler, context), context), context)
            End If
        End Sub

        Public Overloads Sub SendTab()
            Dim handler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of TabKeyCommandArgs))()
            MyBase.SendTab(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn()
            Dim handler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of ReturnKeyCommandArgs))()
            MyBase.SendReturn(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertNewLine())
        End Sub

        Public Overrides Sub SendBackspace()
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of BackspaceKeyCommandArgs))()
            MyBase.SendBackspace(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendBackspace)
        End Sub

        Public Overrides Sub SendDelete()
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of DeleteKeyCommandArgs))()
            MyBase.SendDelete(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDelete)
        End Sub

        Public Sub SendDeleteToSpecificViewAndBuffer(view As IWpfTextView, buffer As ITextBuffer)
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of DeleteKeyCommandArgs))()
            compHandler.ExecuteCommand(New DeleteKeyCommandArgs(view, buffer), AddressOf MyBase.SendDelete, TestCommandExecutionContext.Create())
        End Sub

        Private Overloads Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext)
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))()
            ExecuteTypeCharCommand(args, finalHandler, context, compHandler)
        End Sub

        Public Overloads Sub SendTypeChars(typeChars As String)
            MyBase.SendTypeChars(typeChars, Sub(a, n, c) ExecuteTypeCharCommand(a, n, c))
        End Sub

        Public MustOverride Overloads Sub SendCut()

        Public MustOverride Overloads Sub SendPaste()

        Public MustOverride Overloads Sub SendEscape()

        Public MustOverride Overloads Sub SendDownKey()

        Public MustOverride Overloads Sub SendUpKey()

        Public MustOverride Overloads Sub SendPageUp()

        Public MustOverride Overloads Sub SendInsertSnippetCommand()

        Public MustOverride Overloads Sub SendSurroundWithCommand()

        Public MustOverride Overloads Sub SendInvokeCompletionList()

        Public MustOverride Overloads Sub SendSave()

        Public MustOverride Overloads Sub SendSelectAll()

        Protected MustOverride Function GetHandler(Of T As VSCommanding.ICommandHandler)() As T

#End Region

#Region "Completion Operations"

        Public MustOverride Function GetSelectedItem() As CompletionItem

        Public MustOverride Sub CalculateItemsIfSessionExists()

        Public MustOverride Function GetCompletionItems() As IList(Of CompletionItem)

        Public MustOverride Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)

        Public MustOverride Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)

        Public MustOverride Sub AssertCompletionItemExpander(isAvailable As Boolean, isSelected As Boolean)

        Public MustOverride Sub SetCompletionItemExpanderState(isSelected As Boolean)

        Public MustOverride Function HasSuggestedItem() As Boolean

        Public MustOverride Function IsSoftSelected() As Boolean

        Public MustOverride Overloads Sub SendCommitUniqueCompletionListItem()

        Public MustOverride Overloads Sub SendSelectCompletionItem(displayText As String)

        Public MustOverride Overloads Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)

        Public MustOverride Function AssertNoCompletionSession() As Task

        Public MustOverride Sub AssertNoCompletionSessionWithNoBlock()

        Public MustOverride Function AssertCompletionSessionAfterTypingHash() As Task

        Public MustOverride Overloads Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task

        Public MustOverride Function AssertCompletionItemsContainAll(displayText As String()) As Task

        Public MustOverride Function AssertCompletionItemsContain(displayText As String, displayTextSuffix As String) As Task

        Public MustOverride Function AssertCompletionItemsDoNotContainAny(displayText As String()) As Task

        Public MustOverride Overloads Sub AssertItemsInOrder(expectedOrder As String())

        Public MustOverride Overloads Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task

        Public MustOverride Overloads Sub ToggleSuggestionMode()

        Public Overloads Sub SendTypeCharsToSpecificViewAndBuffer(typeChars As String, view As IWpfTextView, buffer As ITextBuffer)
            For Each ch In typeChars
                Dim localCh = ch
                ExecuteTypeCharCommand(New TypeCharCommandArgs(view, buffer, localCh), Sub() EditorOperations.InsertText(localCh.ToString()), TestCommandExecutionContext.Create())
            Next
        End Sub

        Public Async Function AssertLineTextAroundCaret(expectedTextBeforeCaret As String, expectedTextAfterCaret As String) As Task
            Await WaitForAsynchronousOperationsAsync()

            Dim actual = GetLineTextAroundCaretPosition()

            Assert.Equal(expectedTextBeforeCaret, actual.TextBeforeCaret)
            Assert.Equal(expectedTextAfterCaret, actual.TextAfterCaret)
        End Function

        Public MustOverride Overloads Function AssertSelectedCompletionItem(
                               Optional displayText As String = Nothing,
                               Optional displayTextSuffix As String = Nothing,
                               Optional description As String = Nothing,
                               Optional isSoftSelected As Boolean? = Nothing,
                               Optional isHardSelected As Boolean? = Nothing,
                               Optional shouldFormatOnCommit As Boolean? = Nothing,
                               Optional inlineDescription As String = Nothing,
                               Optional automationText As String = Nothing,
                               Optional projectionsView As ITextView = Nothing) As Task

        Public MustOverride Function WaitForUIRenderedAsync() As Task

        Public Sub NavigateToDisplayText(targetText As String)
            Dim currentText = GetSelectedItem().DisplayText

            ' GetComputedItems provided by the Editor for tests does not guarantee that 
            ' the order of items match the order of items actually displayed in the completion popup.
            ' For example, they put starred items (intellicode) below non-starred ones.
            ' And the order they display those items in the UI is opposite.
            ' Therefore, we do the full traverse: down to the bottom and if not found up to the top.
            Do While currentText <> targetText
                SendDownKey()
                Dim newText = GetSelectedItem().DisplayText
                If currentText = newText Then
                    ' Nothing found on going down. Try going up
                    Do While currentText <> targetText
                        SendUpKey()
                        newText = GetSelectedItem().DisplayText
                        Assert.True(newText <> currentText, "Reached the bottom, then the top and didn't find the match")
                        currentText = newText
                    Loop
                End If

                currentText = newText
            Loop
        End Sub

#End Region

#Region "Signature Help Operations"

        Public Overloads Sub SendInvokeSignatureHelp()
            Dim handler = DirectCast(SignatureHelpBeforeCompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeSignatureHelpCommandArgs))
            MyBase.SendInvokeSignatureHelp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Async Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If

            Assert.Null(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Overloads Async Function AssertSignatureHelpSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Overloads Function GetSignatureHelpItems() As IList(Of SignatureHelpItem)
            Return CurrentSignatureHelpPresenterSession.SignatureHelpItems
        End Function

        Public Async Function AssertSignatureHelpItemsContainAll(displayText As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.True(displayText.All(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                            Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v)))
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

#Region "Helpers"

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
                GetType(IntelliSenseTestState),
                GetType(MockCompletionPresenterProvider)
            }

            If extraExportedTypes IsNot Nothing Then
                result.AddRange(extraExportedTypes)
            End If

            Return result
        End Function

        Private Shared Function GetDisplayText(item As SignatureHelpItem, selectedParameter As Integer) As String
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

        Private Shared Function GetDisplayText(parts As IEnumerable(Of TaggedText)) As String
            Return String.Join(String.Empty, parts.Select(Function(p) p.ToString()))
        End Function

#End Region

    End Class
End Namespace
