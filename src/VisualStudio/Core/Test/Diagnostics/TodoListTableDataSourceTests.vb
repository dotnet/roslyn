﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.TodoComments
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
Imports Microsoft.VisualStudio.Shell.TableManager
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <[UseExportProvider]>
    Public Class TodoListTableDataSourceTests
        <Fact>
        Public Sub TestCreation()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim provider = New TestTodoListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Assert.Equal(manager.Identifier, StandardTables.TasksTable)
                Assert.Equal(1, manager.Sources.Count())

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                AssertEx.SetEqual(table.Columns, manager.GetColumnsForSources(SpecializedCollections.SingletonEnumerable(source)))

                Assert.Equal(ServicesVSResources.CSharp_VB_Todo_List_Table_Data_Source, source.DisplayName)
                Assert.Equal(StandardTableDataSources.CommentTableDataSource, source.SourceTypeIdentifier)

                Assert.Equal(1, manager.Sinks_TestOnly.Count())

                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()
                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Assert.Equal(0, sink.Entries.Count())
                Assert.Equal(1, source.NumberOfSubscription_TestOnly)

                subscription.Dispose()
                Assert.Equal(0, source.NumberOfSubscription_TestOnly)
            End Using
        End Sub

        <Fact>
        Public Sub TestInitialEntries()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestTodoListProvider(CreateItem(documentId))
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Assert.Equal(1, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntryChanged()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestTodoListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)

                provider.Items = New TodoCommentData() {CreateItem(documentId)}
                provider.RaiseTodoListUpdated(workspace)
                Assert.Equal(1, sink.Entries.Count)

                provider.Items = Array.Empty(Of TodoCommentData)()
                provider.RaiseClearTodoListUpdated(workspace, documentId)
                Assert.Equal(0, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)

                Dim filename = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.Span.Path, filename)

                Dim text = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(item.MappedSpan.StartLinePosition.Line, line)

                Dim column = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(item.MappedSpan.StartLinePosition.Character, column)
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(snapshot1.VersionNumber + 1, snapshot2.VersionNumber)

                Assert.Equal(1, snapshot1.Count)

                Dim filename = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.Span.Path, filename)

                Dim text = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(item.MappedSpan.StartLinePosition.Line, line)

                Dim column = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(item.MappedSpan.StartLinePosition.Character, column)
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(0, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo2()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                Dim pos = New LinePosition(11, 21)
                Dim span2 = New FileLinePositionSpan("test2", pos, pos)
                Dim span1 = New FileLinePositionSpan("test1", pos, pos)
                provider.Items = New TodoCommentData() {
                    New TodoCommentData(priority:=1, message:="test2", documentId:=documentId, span:=span2, mappedSpan:=span2),
                    New TodoCommentData(priority:=0, message:="test", documentId:=documentId, span:=span1, mappedSpan:=span1)
                }

                provider.RaiseTodoListUpdated(workspace)

                Dim snapshot2 = factory.GetCurrentSnapshot()
                Assert.Equal(1, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo3()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                Dim pos = New LinePosition(11, 21)
                Dim span2 = New FileLinePositionSpan("test2", pos, pos)
                Dim span3 = New FileLinePositionSpan("test3", pos, pos)
                provider.Items = New TodoCommentData() {
                    New TodoCommentData(priority:=1, message:="test2", documentId:=documentId, span:=span2, mappedSpan:=span2),
                    New TodoCommentData(priority:=0, message:="test3", documentId:=documentId, span:=span3, mappedSpan:=span3)
                }

                provider.RaiseTodoListUpdated(workspace)

                Dim snapshot2 = factory.GetCurrentSnapshot()
                Assert.Equal(-1, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestInvalidEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)

                Dim temp = Nothing
                Assert.False(snapshot.TryGetValue(-1, StandardTableKeyNames.DocumentName, temp))
                Assert.False(snapshot.TryGetValue(1, StandardTableKeyNames.DocumentName, temp))
                Assert.False(snapshot.TryGetValue(0, "Test", temp))
            End Using
        End Sub

        <Fact>
        Public Sub TestAggregatedEntries()
            Dim markup = <Workspace>
                             <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                                 <Document FilePath="test1"><![CDATA[// TODO hello]]></Document>
                             </Project>
                             <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="test1"/>
                             </Project>
                         </Workspace>

            Using workspace = TestWorkspace.Create(markup)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim projects = workspace.CurrentSolution.Projects.ToArray()

                Dim item1 = CreateItem(projects(0).DocumentIds.First())
                Dim item2 = CreateItem(projects(1).DocumentIds.First())

                Dim provider = New TestTodoListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTableWorkspaceEventListener.VisualStudioTodoListTable(workspace, threadingContext, provider, tableManagerProvider)

                provider.Items = New TodoCommentData() {item1, item2}
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoTableItem, TodoItemsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim filename As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal("test1", filename)

                Dim projectname As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, projectname))
                Assert.Equal("Proj1, Proj2", projectname)

                Dim projectnames As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName + "s", projectnames))
                Assert.Equal(2, DirectCast(projectnames, String()).Length)

                Dim projectguid As Object = Nothing
                Assert.False(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectGuid, projectguid))
            End Using
        End Sub

        Private Shared Function CreateItem(documentId As DocumentId) As TodoCommentData
            Dim pos = New LinePosition(10, 20)
            Dim span = New FileLinePositionSpan("test1", pos, pos)
            Return New TodoCommentData(
                priority:=0,
                message:="test",
                documentId:=documentId,
                span:=span,
                mappedSpan:=span)
        End Function

        Private Class TestTodoListProvider
            Implements ITodoListProvider

            Public Items As TodoCommentData()

            Public Sub New(ParamArray items As TodoCommentData())
                Me.Items = items
            End Sub

            Public Event TodoListUpdated As EventHandler(Of TodoItemsUpdatedArgs) Implements ITodoListProvider.TodoListUpdated

            Public Function GetTodoItems(workspace As Workspace, documentId As DocumentId, cancellationToken As CancellationToken) As ImmutableArray(Of TodoCommentData) Implements ITodoListProvider.GetTodoItems
                Assert.NotNull(workspace)
                Assert.NotNull(documentId)

                Return Items.Where(Function(t) t.DocumentId Is documentId).ToImmutableArrayOrEmpty()
            End Function

            Public Sub RaiseTodoListUpdated(workspace As Workspace)
                Dim map = Items.ToLookup(Function(t) t.DocumentId)

                For Each group In map
                    RaiseEvent TodoListUpdated(Me, New TodoItemsUpdatedArgs(
                        Me, workspace.CurrentSolution, group.Key, group.ToImmutableArrayOrEmpty()))
                Next
            End Sub

            Public Sub RaiseClearTodoListUpdated(workspace As Microsoft.CodeAnalysis.Workspace, documentId As DocumentId)
                RaiseEvent TodoListUpdated(Me, New TodoItemsUpdatedArgs(
                    Me, workspace.CurrentSolution, documentId, ImmutableArray(Of TodoCommentData).Empty))
            End Sub
        End Class
    End Class
End Namespace
