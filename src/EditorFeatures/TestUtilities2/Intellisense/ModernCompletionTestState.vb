' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Partial Friend Class ModernCompletionTestState
        Inherits TestStateBase

        Friend Const RoslynItem = "RoslynItem"
        Friend ReadOnly EditorCompletionCommandHandler As VSCommanding.ICommandHandler

        Private Sub New(workspaceElement As XElement,
                        extraCompletionProviders As IEnumerable(Of Lazy(Of CompletionProvider, OrderableLanguageAndRoleMetadata)),
                        Optional excludedTypes As List(Of Type) = Nothing,
                        Optional extraExportedTypes As List(Of Type) = Nothing,
                        Optional includeFormatCommandHandler As Boolean = False,
                        Optional workspaceKind As String = Nothing)

            MyBase.New(workspaceElement, extraCompletionProviders, excludedTypes, extraExportedTypes, includeFormatCommandHandler, workspaceKind)

            EditorCompletionCommandHandler = GetExportedValues(Of VSCommanding.ICommandHandler)().
                Single(Function(e As VSCommanding.ICommandHandler) e.GetType().FullName = "Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation.CompletionCommandHandler")
        End Sub

        Public Shared Function CreateVisualBasicTestState(
                documentElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing) As ModernCompletionTestState
            Return New ModernCompletionTestState(
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

        Public Shared Function CreateCSharpTestState(
                documentElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional excludedTypes As List(Of Type) = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing,
                Optional includeFormatCommandHandler As Boolean = False) As ModernCompletionTestState
            Return New ModernCompletionTestState(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            <%= documentElement.Value %>
                        </Document>
                    </Project>
                </Workspace>,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.CSharp, roles:=Nothing), ' Add in the Editor Completion mef components; Get the completion iolecommandtarget to send commands to and do normal assertions based on the old (/current) CompletionBroker
                excludedTypes,
                extraExportedTypes,
                includeFormatCommandHandler)
        End Function

        Public Shared Function CreateTestStateFromWorkspace(
                workspaceElement As XElement,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraExportedTypes As List(Of Type) = Nothing,
                Optional workspaceKind As String = Nothing) As ModernCompletionTestState
            Return New ModernCompletionTestState(
                workspaceElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                excludedTypes:=Nothing,
                extraExportedTypes,
                workspaceKind:=workspaceKind)
        End Function

#Region "Editor Related Operations"

        Public Overrides Sub SendEscape()
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of EscapeKeyCommandArgs))
            MyBase.SendEscape(Sub(a, n, c) handler.ExecuteCommand(a, Sub() IntelliSenseCommandHandler.ExecuteCommand(a, n, c), c), Sub() Return)
        End Sub

        Public Overrides Sub SendDownKey()
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of DownKeyCommandArgs))
            MyBase.SendDownKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, Sub() handler.ExecuteCommand(a, n, c), c), Sub() Return)
        End Sub

        Public Overrides Sub SendUpKey()
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of UpKeyCommandArgs))
            MyBase.SendUpKey(Sub(a, n, c) IntelliSenseCommandHandler.ExecuteCommand(a, Sub() handler.ExecuteCommand(a, n, c), c), Sub() Return)
        End Sub

        Public Overrides Sub SendPageUp()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of PageUpKeyCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendCut()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of CutCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of CutCommandArgs))
            MyBase.SendCut(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendPaste()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of PasteCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of PasteCommandArgs))
            MyBase.SendPaste(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendInvokeCompletionList()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of InvokeCompletionListCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendInsertSnippetCommand()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of InsertSnippetCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of InsertSnippetCommandArgs))
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSurroundWithCommand()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of SurroundWithCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of SurroundWithCommandArgs))
            MyBase.SendSurroundWithCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSave()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of SaveCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of SaveCommandArgs))
            MyBase.SendSave(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSelectAll()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of SelectAllCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of SelectAllCommandArgs))
            MyBase.SendSelectAll(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendDeleteWordToLeft()
            Dim compHandler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of WordDeleteToStartCommandArgs))
            MyBase.SendWordDeleteToStart(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDeleteWordToLeft)
        End Sub

        Protected Overrides Function GetHandler(Of T As VSCommanding.ICommandHandler)() As T
            Return DirectCast(EditorCompletionCommandHandler, T)
        End Function

#End Region

#Region "Completion Operations"

        Public Overrides Sub SendCommitUniqueCompletionListItem()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of CommitUniqueCompletionListItemCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Async Function AssertNoCompletionSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If (session Is Nothing) Then
                Return
            End If

            If (session.IsDismissed) Then
                Return
            End If

            ' If completionItems cannot be calculated in 5 seconds, no session exists.
            If Not block Then
                Dim task1 = New Task(Sub()
                                         Thread.Sleep(5000)
                                     End Sub)
                task1.Start()

                Dim task2 = New Task(Sub()
                                         Dim completionItems = session.GetComputedItems(CancellationToken.None)
                                         ' In the non blocking mode, we are not interested for a session appeared later than in 5 seconds.
                                         If task1.Status = TaskStatus.Running Then
                                             Assert.True(session.IsDismissed OrElse completionItems.Items.Count() = 0)
                                         End If
                                     End Sub)
                task2.Start()

                Task.WaitAny(task1, task2)
            Else
                Dim completionItems = session.GetComputedItems(CancellationToken.None)
                Assert.True(session.IsDismissed OrElse completionItems.Items.Count() = 0)
            End If
        End Function

        Public Overrides Async Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task
            Dim view = If(projectionsView, TextView)

            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.NotNull(session)
        End Function

        Public Overrides Function CompletionItemsContainsAll(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)
            Return displayText.All(Function(v) items.Items.Any(Function(i) i.DisplayText = v))
        End Function

        Public Overrides Function CompletionItemsContainsAny(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)

            Return displayText.Any(Function(v) items.Items.Any(
                                       Function(i) i.DisplayText = v))
        End Function

        Public Overrides Sub AssertItemsInOrder(expectedOrder As String())
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None).Items
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
            Dim view = If(projectionsView, TextView)

            Await WaitForAsynchronousOperationsAsync()

            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)

            If isSoftSelected.HasValue Then
                If isSoftSelected.Value Then
                    Assert.True(items.UsesSoftSelection, "Current completion is not soft-selected. Expected: soft-selected")
                Else
                    Assert.False(items.UsesSoftSelection, "Current completion is soft-selected. Expected: not soft-selected")
                End If
            End If

            If isHardSelected.HasValue Then
                If isHardSelected.Value Then
                    Assert.True(Not items.UsesSoftSelection, "Current completion is not hard-selected. Expected: hard-selected")
                Else
                    Assert.True(items.UsesSoftSelection, "Current completion is hard-selected. Expected: not hard-selected")
                End If
            End If

            If displayText IsNot Nothing Then
                Assert.NotNull(items.SelectedItem)
                Assert.Equal(displayText, items.SelectedItem.DisplayText)
            End If

            If displayTextSuffix IsNot Nothing Then
                Assert.NotNull(items.SelectedItem)
                Assert.Equal(displayTextSuffix, items.SelectedItem.Suffix)
            End If

            If shouldFormatOnCommit.HasValue Then
                Assert.Equal(shouldFormatOnCommit.Value, GetRoslynCompletionItem(items.SelectedItem).Rules.FormatOnCommit)
            End If

            If description IsNot Nothing Then
                Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim roslynItem = GetRoslynCompletionItem(items.SelectedItem)
                Dim itemDescription = Await service.GetDescriptionAsync(document, roslynItem)
                Assert.Equal(description, itemDescription.Text)
            End If
        End Function

        Public Overrides Async Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If Not session Is Nothing Then
                Assert.False(CompletionItemsContainsAny({"ClassLibrary1"}))
            End If
        End Function

        Public Overrides Function GetSelectedItem() As CompletionItem
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)
            Return GetRoslynCompletionItem(items.SelectedItem)
        End Function

        Public Overrides Function GetSelectedItemOpt() As CompletionItem
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If session IsNot Nothing Then
                Dim item = session.GetComputedItems(CancellationToken.None).SelectedItem
                Dim completionItem As CompletionItem = Nothing
                If item?.Properties.TryGetProperty(RoslynItem, completionItem) Then
                    Return completionItem
                End If
            End If

            Return Nothing
        End Function

        Public Overrides Function GetCompletionItems() As IList(Of CompletionItem)
            Return New List(Of CompletionItem)(GetCompletionItemsAsync().Result)
        End Function

        Private Async Function GetCompletionItemsAsync() As Task(Of IEnumerable(Of CompletionItem))
            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Return session.GetComputedItems(CancellationToken.None).Items.Select(Function(item) GetRoslynCompletionItem(item))
        End Function

        Private Shared Function GetRoslynCompletionItem(item As Data.CompletionItem) As CompletionItem
            Return DirectCast(item.Properties("RoslynItem"), CompletionItem)
        End Function

        Public Overrides Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function HasSuggestedItem() As Boolean
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.SuggestionItem IsNot Nothing
        End Function

        Public Overrides Function IsSoftSelected() As Boolean
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.UsesSoftSelection
        End Function

        Public Overrides Sub SendSelectCompletionItem(displayText As String)
            AssertNoAsynchronousOperationsRunning()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Dim operations = DirectCast(session, IAsyncCompletionSessionOperations)
            operations.SelectCompletionItem(session.GetComputedItems(CancellationToken.None).Items.Single(Function(i) i.DisplayText = displayText))
        End Sub

        Public Overrides Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)
            Throw New NotImplementedException()
        End Sub

#End Region
    End Class
End Namespace
