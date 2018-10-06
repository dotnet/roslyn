' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend MustInherit Class TestStateBase
        Inherits AbstractCommandHandlerTestState

        Friend ReadOnly AsyncCompletionService As IAsyncCompletionService
        Friend ReadOnly SignatureHelpCommandHandler As SignatureHelpCommandHandler
        Friend ReadOnly FormatCommandHandler As FormatCommandHandler
        Friend ReadOnly IntelliSenseCommandHandler As IntelliSenseCommandHandler
        Protected ReadOnly SessionTestState As IIntelliSenseTestState

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        Public Sub New(workspaceElement As XElement,
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

            Me.SignatureHelpCommandHandler = GetExportedValue(Of SignatureHelpCommandHandler)()

            Me.IntelliSenseCommandHandler = GetExportedValue(Of IntelliSenseCommandHandler)()

            Me.FormatCommandHandler = If(includeFormatCommandHandler, GetExportedValue(Of FormatCommandHandler)(), Nothing)
        End Sub

#Region "Editor Related Operations"
        Public MustOverride Overloads Sub SendEscape()

        Public MustOverride Overloads Sub SendDownKey()

        Public MustOverride Overloads Sub SendUpKey()

        Public MustOverride Overloads Sub SendTypeChars(typeChars As String)

        Public MustOverride Overloads Sub SendTab()

        Public MustOverride Overloads Sub SendReturn()

        Public MustOverride Overloads Sub SendPageUp()

        Public MustOverride Overloads Sub SendCut()

        Public MustOverride Overloads Sub SendPaste()

        Public MustOverride Overloads Sub SendInvokeCompletionList()

        Public MustOverride Overloads Sub SendInsertSnippetCommand()

        Public MustOverride Overloads Sub SendSurroundWithCommand()

        Public MustOverride Overloads Sub SendSave()

        Public MustOverride Overloads Sub SendSelectAll()

#End Region

#Region "Completion Operations"

        Public MustOverride Sub SendDeleteToSpecificViewAndBuffer(view As IWpfTextView, buffer As ITextBuffer)

        Public MustOverride Function GetSelectedItem() As CompletionItem

        Public MustOverride Function GetSelectedItemOpt() As CompletionItem

        Public MustOverride Function GetCompletionItems() As IList(Of CompletionItem)

        Public MustOverride Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)

        Public MustOverride Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)

        Public MustOverride Function HasSuggestedItem() As Boolean

        Public MustOverride Function IsSoftSelected() As Boolean

        Public MustOverride Overloads Sub SendCommitUniqueCompletionListItem()

        Public MustOverride Overloads Sub SendSelectCompletionItem(displayText As String)

        Public MustOverride Overloads Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)

        Public MustOverride Function AssertNoCompletionSession(Optional block As Boolean = True) As Task

        Public MustOverride Overloads Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task

        Public MustOverride Overloads Function CompletionItemsContainsAll(displayText As String()) As Boolean

        Public MustOverride Overloads Function CompletionItemsContainsAny(displayText As String()) As Boolean

        Public MustOverride Overloads Sub AssertItemsInOrder(expectedOrder As String())

        Public MustOverride Overloads Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task

        Public MustOverride Overloads Sub SendTypeCharsToSpecificViewAndBuffer(typeChars As String, view As IWpfTextView, buffer As ITextBuffer)

        Public Async Function AssertLineTextAroundCaret(expectedTextBeforeCaret As String, expectedTextAfterCaret As String) As Task
            Await WaitForAsynchronousOperationsAsync()

            Dim actual = GetLineTextAroundCaretPosition()

            Assert.Equal(expectedTextBeforeCaret, actual.TextBeforeCaret)
            Assert.Equal(expectedTextAfterCaret, actual.TextAfterCaret)
        End Function

        Public MustOverride Overloads Function AssertSelectedCompletionItem(
                               Optional displayText As String = Nothing,
                               Optional description As String = Nothing,
                               Optional isSoftSelected As Boolean? = Nothing,
                               Optional isHardSelected As Boolean? = Nothing,
                               Optional shouldFormatOnCommit As Boolean? = Nothing,
                               Optional projectionsView As ITextView = Nothing) As Task

#End Region

#Region "Signature Help Operations"

        Public MustOverride Overloads Sub SendInvokeSignatureHelp()

        Public MustOverride Overloads Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task

        Public MustOverride Overloads Function AssertSignatureHelpSession() As Task

        Public MustOverride Function GetSignatureHelpItems() As IList(Of SignatureHelpItem)

        Public Function SignatureHelpItemsContainsAll(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
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
                GetType(IntelliSenseTestState)
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
