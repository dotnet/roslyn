' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding
Imports Microsoft.CodeAnalysis.CSharp

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Partial Friend Class TestState
        Inherits TestStateBase

        Friend ReadOnly CompletionCommandHandler As CompletionCommandHandler

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
            MyBase.New(workspaceElement, extraCompletionProviders, excludedTypes, extraExportedTypes, includeFormatCommandHandler, workspaceKind)

            Me.CompletionCommandHandler = GetExportedValue(Of CompletionCommandHandler)()
        End Sub

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

#Region "Editor Related Operations"

        Public Overrides Sub SendEscape()
            MyBase.SendEscape(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendDownKey()
            MyBase.SendDownKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendUpKey()
            MyBase.SendUpKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendPageUp()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendCut()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CutCommandArgs))
            MyBase.SendCut(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendPaste()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PasteCommandArgs))
            MyBase.SendPaste(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendInvokeCompletionList()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub
        Public Overrides Sub SendInsertSnippetCommand()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InsertSnippetCommandArgs))
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSurroundWithCommand()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SurroundWithCommandArgs))
            MyBase.SendSurroundWithCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSave()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SaveCommandArgs))
            MyBase.SendSave(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSelectAll()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SelectAllCommandArgs))
            MyBase.SendSelectAll(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Protected Overrides Function GetHandler(Of T As VSCommanding.ICommandHandler)() As T
            Return DirectCast(DirectCast(CompletionCommandHandler, VSCommanding.ICommandHandler), T)
        End Function
#End Region

#Region "Completion Operations"

        Public Overrides Function GetSelectedItem() As CompletionItem
            Return CurrentCompletionPresenterSession.SelectedItem
        End Function

        Public Overrides Function GetSelectedItemOpt() As CompletionItem
            Return CurrentCompletionPresenterSession?.SelectedItem
        End Function

        Public Overrides Function GetCompletionItems() As IList(Of CompletionItem)
            Return CurrentCompletionPresenterSession.CompletionItems
        End Function

        Public Overrides Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)
            CurrentCompletionPresenterSession.RaiseFiltersChanged(args)
        End Sub

        Public Overrides Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)
            Return CurrentCompletionPresenterSession.CompletionItemFilters
        End Function

        Public Overrides Function HasSuggestedItem() As Boolean
            Return CurrentCompletionPresenterSession.SuggestionModeItem IsNot Nothing
        End Function

        Public Overrides Function IsSoftSelected() As Boolean
            Return CurrentCompletionPresenterSession.IsSoftSelected
        End Function


        Public Overrides Sub SendCommitUniqueCompletionListItem()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub
        Public Overrides Sub SendSelectCompletionItem(displayText As String)
            Dim item = CurrentCompletionPresenterSession.CompletionItems.FirstOrDefault(Function(i) i.DisplayText = displayText)
            Assert.NotNull(item)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overrides Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overrides Async Function AssertNoCompletionSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If
            Assert.Null(Me.CurrentCompletionPresenterSession)
        End Function

        Public Overrides Async Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task
            ' projectionsView is not used in this implementation
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentCompletionPresenterSession)
        End Function

        Public Overrides Function CompletionItemsContainsAll(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentCompletionPresenterSession.CompletionItems.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Overrides Function CompletionItemsContainsAny(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.Any(Function(v) CurrentCompletionPresenterSession.CompletionItems.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Overrides Sub AssertItemsInOrder(expectedOrder As String())
            AssertNoAsynchronousOperationsRunning()
            Dim items = CurrentCompletionPresenterSession.CompletionItems
            Assert.Equal(expectedOrder.Count, items.Count)
            For i = 0 To expectedOrder.Count - 1
                Assert.Equal(expectedOrder(i), items(i).DisplayText)
            Next
        End Sub

        Public Overrides Async Function AssertSelectedCompletionItem(
                               Optional displayText As String = Nothing,
                               Optional displayTextSuffix As String = Nothing,
                               Optional description As String = Nothing,
                               Optional isSoftSelected As Boolean? = Nothing,
                               Optional isHardSelected As Boolean? = Nothing,
                               Optional shouldFormatOnCommit As Boolean? = Nothing,
                               Optional projectionsView As ITextView = Nothing) As Task
            ' projectionsView is not used in this implementation.

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

            If description IsNot Nothing Then
                Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim itemDescription = Await service.GetDescriptionAsync(
                    document, Me.CurrentCompletionPresenterSession.SelectedItem)
                Assert.Equal(description, itemDescription.Text)
            End If
        End Function

        Public Overrides Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task
            If Not CurrentCompletionPresenterSession Is Nothing Then
                Assert.False(CompletionItemsContainsAny({"ClassLibrary1"}))
            End If

            Return Task.FromResult(0)
        End Function

#End Region
    End Class
End Namespace
