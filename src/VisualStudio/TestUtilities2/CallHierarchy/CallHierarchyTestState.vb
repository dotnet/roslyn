' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
Imports Microsoft.CodeAnalysis.Editor.Implementation.Notification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.SymbolMapping
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.CallHierarchy
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy
    Public Class CallHierarchyTestState
        Private Shared ReadOnly DefaultCatalog As ComposableCatalog = TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic _
                .WithPart(GetType(CallHierarchyProvider)) _
                .WithPart(GetType(DefaultSymbolMappingService)) _
                .WithPart(GetType(EditorNotificationServiceFactory))
        Private Shared ReadOnly ExportProviderFactory As IExportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(DefaultCatalog)

        Private ReadOnly _commandHandler As CallHierarchyCommandHandler
        Private ReadOnly _presenter As MockCallHierarchyPresenter
        Friend ReadOnly Workspace As TestWorkspace
        Private ReadOnly _subjectBuffer As ITextBuffer
        Private ReadOnly _textView As IWpfTextView

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

        Public Shared Function Create(markup As XElement, ParamArray additionalTypes As Type()) As CallHierarchyTestState
            Dim exportProvider = CreateExportProvider(additionalTypes)
            Dim Workspace = TestWorkspace.Create(markup, exportProvider:=exportProvider)

            Return New CallHierarchyTestState(Workspace)
        End Function

        Private Sub New(workspace As TestWorkspace)
            Me.Workspace = workspace
            Dim testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)

            _textView = testDocument.GetTextView()
            _subjectBuffer = testDocument.GetTextBuffer()

            Dim provider = workspace.GetService(Of CallHierarchyProvider)()

            Dim notificationService = DirectCast(workspace.Services.GetService(Of INotificationService)(), INotificationServiceCallback)
            notificationService.NotificationCallback = Sub(message, title, severity) NotificationMessage = message

            _presenter = New MockCallHierarchyPresenter()
            _commandHandler = New CallHierarchyCommandHandler({_presenter}, provider)
        End Sub

        Private Shared Function CreateExportProvider(additionalTypes As IEnumerable(Of Type)) As ExportProvider
            If Not additionalTypes.Any Then
                Return ExportProviderFactory.CreateExportProvider()
            End If

            Dim catalog = DefaultCatalog.WithParts(additionalTypes)
            Return ExportProviderCache.GetOrCreateExportProviderFactory(catalog).CreateExportProvider()
        End Function

        Public Shared Function Create(markup As String, ParamArray additionalTypes As Type()) As CallHierarchyTestState
            Dim exportProvider = CreateExportProvider(additionalTypes)
            Dim workspace = TestWorkspace.CreateCSharp(markup, exportProvider:=exportProvider)
            Return New CallHierarchyTestState(workspace)
        End Function

        Friend Property NotificationMessage As String

        Friend Function GetRoot() As CallHierarchyItem
            Dim args = New ViewCallHierarchyCommandArgs(_textView, _subjectBuffer)
            _commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create())
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

        Private Sub SearchRoot(root As CallHierarchyItem, displayName As String, callback As MockSearchCallback, scope As CallHierarchySearchScope, documents As IImmutableSet(Of Document))
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
            SearchRoot(root, searchCategory, Sub(c As ICallHierarchyNameItem)
                                                 Assert.Contains(ConvertToName(c), expectedCallers)
                                             End Sub,
                scope,
                documents)
        End Sub

        Friend Sub VerifyResult(root As CallHierarchyItem, searchCategory As String, expectedCallers As String(), Optional scope As CallHierarchySearchScope = CallHierarchySearchScope.EntireSolution, Optional documents As IImmutableSet(Of Document) = Nothing)
            SearchRoot(root, searchCategory, Sub(c As CallHierarchyItem)
                                                 Assert.Contains(ConvertToName(c), expectedCallers)
                                             End Sub,
                scope,
                documents)
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
    End Class
End Namespace
