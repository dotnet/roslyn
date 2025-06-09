' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Language.CallHierarchy
Imports Microsoft.VisualStudio.LanguageServices.UnitTests
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy
    Public Class CallHierarchyTestState
        Implements IDisposable

        Private ReadOnly _commandHandler As CallHierarchyCommandHandler
        Private ReadOnly _presenter As MockCallHierarchyPresenter
        Friend ReadOnly Workspace As EditorTestWorkspace
        Private ReadOnly _subjectBuffer As ITextBuffer
        Private ReadOnly _textView As IWpfTextView
        Private ReadOnly _waiter As IAsynchronousOperationWaiter

        Private Class MockCallHierarchyPresenter
            Implements ICallHierarchyPresenter

            Public PresentedRoot As CallHierarchyItem

            Public Sub PresentRoot(root As CallHierarchyItem) Implements ICallHierarchyPresenter.PresentRoot
                Me.PresentedRoot = root
            End Sub
        End Class

        Friend Class MockSearchCallback
            Implements ICallHierarchySearchCallback

            Private ReadOnly _verifyMemberItem As Action(Of CallHierarchyItem)
            Private ReadOnly _completionSource As TaskCompletionSource(Of Object) = New TaskCompletionSource(Of Object)()
            Private ReadOnly _verifyNameItem As Action(Of ICallHierarchyNameItem)

            Public Sub New(verify As Action(Of CallHierarchyItem))
                _verifyMemberItem = verify
            End Sub

            Public Sub New(verify As Action(Of ICallHierarchyNameItem))
                _verifyNameItem = verify
            End Sub

            Public Sub AddResult(item As ICallHierarchyNameItem) Implements ICallHierarchySearchCallback.AddResult
                _verifyNameItem(item)
            End Sub

            Public Sub AddResult(item As ICallHierarchyMemberItem) Implements ICallHierarchySearchCallback.AddResult
                _verifyMemberItem(DirectCast(item, CallHierarchyItem))
            End Sub

            Public Sub InvalidateResults() Implements ICallHierarchySearchCallback.InvalidateResults
            End Sub

            Public Sub ReportProgress(current As Integer, maximum As Integer) Implements ICallHierarchySearchCallback.ReportProgress
            End Sub

            Public Sub SearchFailed(message As String) Implements ICallHierarchySearchCallback.SearchFailed
                _completionSource.SetException(New Exception(message))
            End Sub

            Public Sub SearchSucceeded() Implements ICallHierarchySearchCallback.SearchSucceeded
                _completionSource.SetResult(Nothing)
            End Sub

            Friend Sub WaitForCompletion()
                _completionSource.Task.Wait()
            End Sub
        End Class

        Private Sub New(workspace As EditorTestWorkspace)
            Me.Workspace = workspace
            Dim testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)

            _textView = testDocument.GetTextView()
            _subjectBuffer = testDocument.GetTextBuffer()

            Dim provider = workspace.GetService(Of CallHierarchyProvider)()

            Dim notificationService = DirectCast(workspace.Services.GetService(Of INotificationService)(), INotificationServiceCallback)
            notificationService.NotificationCallback = Sub(message, title, severity) NotificationMessage = message

            Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
            _presenter = New MockCallHierarchyPresenter()
            Dim threadOperationExecutor = workspace.GetService(Of IUIThreadOperationExecutor)
            Dim asynchronousOperationListenerProvider = workspace.GetService(Of IAsynchronousOperationListenerProvider)()
            _waiter = asynchronousOperationListenerProvider.GetWaiter(FeatureAttribute.CallHierarchy)
            _commandHandler = New CallHierarchyCommandHandler(threadingContext, threadOperationExecutor, asynchronousOperationListenerProvider, {_presenter}, provider)
        End Sub

        Public Shared Function Create(markup As XElement, ParamArray additionalTypes As Type()) As CallHierarchyTestState
            Dim workspace = EditorTestWorkspace.Create(markup, composition:=VisualStudioTestCompositions.LanguageServices.AddParts(additionalTypes))
            Return New CallHierarchyTestState(workspace)
        End Function

        Public Shared Function Create(markup As String, ParamArray additionalTypes As Type()) As CallHierarchyTestState
            Dim workspace = EditorTestWorkspace.CreateCSharp(markup, composition:=VisualStudioTestCompositions.LanguageServices.AddParts(additionalTypes))
            Return New CallHierarchyTestState(workspace)
        End Function

        Friend Property NotificationMessage As String

        Friend Async Function GetRootAsync() As Task(Of CallHierarchyItem)
            Dim args = New ViewCallHierarchyCommandArgs(_textView, _subjectBuffer)
            _commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create())
            Await _waiter.ExpeditedWaitAsync()
            Return _presenter.PresentedRoot
        End Function

        Friend Function GetDocuments(documentNames As String()) As IImmutableSet(Of Document)
            Dim documents = Workspace.CurrentSolution.Projects.SelectMany(Function(p) p.Documents).Where(Function(d) documentNames.Contains(d.Name))
            Return ImmutableHashSet.CreateRange(documents)
        End Function

        Friend Sub SearchRoot(root As CallHierarchyItem, displayName As String, verify As Action(Of CallHierarchyItem), scope As CallHierarchySearchScope, Optional documents As IImmutableSet(Of Document) = Nothing)
            Dim callback = New MockSearchCallback(verify)
            SearchRoot(root, displayName, callback, scope, documents)
        End Sub

        Friend Sub SearchRoot(root As CallHierarchyItem, displayName As String, verify As Action(Of ICallHierarchyNameItem), scope As CallHierarchySearchScope, Optional documents As IImmutableSet(Of Document) = Nothing)
            Dim callback = New MockSearchCallback(verify)
            SearchRoot(root, displayName, callback, scope, documents)
        End Sub

        Private Shared Sub SearchRoot(root As CallHierarchyItem, displayName As String, callback As MockSearchCallback, scope As CallHierarchySearchScope, documents As IImmutableSet(Of Document))
            ' Assert we have the category before we try to find it to give better diagnosing
            Assert.Contains(displayName, root.SupportedSearchCategories.Select(Function(c) c.DisplayName))

            Dim category = root.SupportedSearchCategories.First(Function(c) c.DisplayName = displayName).Name
            If documents IsNot Nothing Then
                root.StartSearchWithDocuments(category, scope, callback, documents)
            Else
                root.StartSearch(category, scope, callback)
            End If

            callback.WaitForCompletion()
        End Sub

        Friend Function ConvertToName(root As ICallHierarchyMemberItem) As String
            Dim name = root.MemberName

            If Not String.IsNullOrEmpty(root.ContainingTypeName) Then
                name = root.ContainingTypeName + "." + name
            End If

            If Not String.IsNullOrEmpty(root.ContainingNamespaceName) Then
                name = root.ContainingNamespaceName + "." + name
            End If

            Return name
        End Function

        Friend Function ConvertToName(root As ICallHierarchyNameItem) As String
            Return root.Name
        End Function

        Friend Sub VerifyRoot(root As CallHierarchyItem, Optional name As String = "", Optional expectedCategories As String() = Nothing)
            Assert.Equal(name, ConvertToName(root))

            If expectedCategories IsNot Nothing Then
                Dim categories = root.SupportedSearchCategories.Select(Function(s) s.DisplayName)
                For Each category In expectedCategories
                    Assert.Contains(category, categories)
                Next
            End If
        End Sub

        Friend Sub VerifyResultName(root As CallHierarchyItem, searchCategory As String, expectedCallers As String(), Optional scope As CallHierarchySearchScope = CallHierarchySearchScope.EntireSolution, Optional documents As IImmutableSet(Of Document) = Nothing)
            Dim callers = New List(Of String)
            SearchRoot(root, searchCategory, Sub(c As ICallHierarchyNameItem)
                                                 callers.Add(ConvertToName(c))
                                             End Sub,
                scope,
                documents)

            Assert.Equal(callers.Count, expectedCallers.Length)
            For Each expected In expectedCallers
                Assert.Contains(expected, callers)
            Next
        End Sub

        Friend Sub VerifyResult(root As CallHierarchyItem, searchCategory As String, expectedCallers As String(), Optional scope As CallHierarchySearchScope = CallHierarchySearchScope.EntireSolution, Optional documents As IImmutableSet(Of Document) = Nothing)
            Dim callers = New List(Of String)
            SearchRoot(root, searchCategory, Sub(c As CallHierarchyItem)
                                                 callers.Add(ConvertToName(c))
                                             End Sub,
                scope,
                documents)

            Assert.Equal(callers.Count, expectedCallers.Length)
            For Each expected In expectedCallers
                Assert.Contains(expected, callers)
            Next

        End Sub

        Friend Sub Navigate(root As CallHierarchyItem, searchCategory As String, callSite As String, Optional scope As CallHierarchySearchScope = CallHierarchySearchScope.EntireSolution, Optional documents As IImmutableSet(Of Document) = Nothing)
            Dim item As CallHierarchyItem = Nothing
            SearchRoot(root, searchCategory, Sub(c As CallHierarchyItem) item = c,
                scope,
                documents)

            If callSite = ConvertToName(item) Then
                Dim detail = item.Details.FirstOrDefault()
                If detail IsNot Nothing Then
                    detail.NavigateTo()
                Else
                    item.NavigateTo()
                End If
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Workspace.Dispose()
        End Sub
    End Class
End Namespace
