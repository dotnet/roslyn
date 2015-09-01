' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
Imports Microsoft.VisualStudio.Shell.TableManager
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class TodoListTableDataSourceTests
        <Fact>
        Public Sub TestCreation()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim provider = New TestTodoListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Assert.Equal(manager.Identifier, StandardTables.TasksTable)
                Assert.Equal(1, manager.Sources.Count())

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                AssertEx.SetEqual(table.Columns, manager.GetColumnsForSources(SpecializedCollections.SingletonEnumerable(source)))

                Assert.Equal(ServicesVSResources.TodoTableSourceName, source.DisplayName)
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
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestTodoListProvider(CreateItem(workspace, documentId))
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Assert.Equal(1, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntryChanged()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestTodoListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)

                provider.Items = New TodoItem() {CreateItem(workspace, documentId)}
                provider.RaiseTodoListUpdated(workspace)
                Assert.Equal(1, sink.Entries.Count)

                provider.Items = Array.Empty(Of TodoItem)()
                provider.RaiseClearTodoListUpdated(workspace, documentId)
                Assert.Equal(0, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntry()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace, documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)

                Dim filename = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.OriginalFilePath, filename)

                Dim text = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(item.MappedLine, line)

                Dim column = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(item.MappedColumn, column)
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotEntry()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace, documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoItem))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(snapshot1.VersionNumber + 1, snapshot2.VersionNumber)

                Assert.Equal(1, snapshot1.Count)

                Dim filename = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.OriginalFilePath, filename)

                Dim text = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(item.MappedLine, line)

                Dim column = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(item.MappedColumn, column)
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace, documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoItem))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(0, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo2()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace, documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoItem))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                provider.Items = New TodoItem() {
                    New TodoItem(1, "test2", workspace, documentId, 11, 11, 21, 21, Nothing, "test2"),
                    New TodoItem(0, "test", workspace, documentId, 11, 11, 21, 21, Nothing, "test1")}

                provider.RaiseTodoListUpdated(workspace)

                Dim snapshot2 = factory.GetCurrentSnapshot()
                Assert.Equal(1, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo3()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace, documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TodoItem))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                provider.Items = New TodoItem() {
                    New TodoItem(1, "test2", workspace, documentId, 11, 11, 21, 21, Nothing, "test2"),
                    New TodoItem(0, "test3", workspace, documentId, 11, 11, 21, 21, Nothing, "test3")}

                provider.RaiseTodoListUpdated(workspace)

                Dim snapshot2 = factory.GetCurrentSnapshot()
                Assert.Equal(-1, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestInvalidEntry()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace, documentId)
                Dim provider = New TestTodoListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTodoListTable(workspace, provider, tableManagerProvider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TodoItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)

                Dim temp = Nothing
                Assert.False(snapshot.TryGetValue(-1, StandardTableKeyNames.DocumentName, temp))
                Assert.False(snapshot.TryGetValue(1, StandardTableKeyNames.DocumentName, temp))
                Assert.False(snapshot.TryGetValue(0, "Test", temp))
            End Using
        End Sub

        Private Function CreateItem(workspace As Workspace, documentId As DocumentId) As TodoItem
            Return New TodoItem(0, "test", workspace, documentId, 10, 10, 20, 20, Nothing, "test1")
        End Function

        Private Class TestTodoListProvider
            Implements ITodoListProvider

            Public Items As TodoItem()

            Public Sub New(ParamArray items As TodoItem())
                Me.Items = items
            End Sub

            Public Event TodoListUpdated As EventHandler(Of TodoListEventArgs) Implements ITodoListProvider.TodoListUpdated

            Public Function GetTodoItems(workspace As Workspace, documentId As DocumentId, cancellationToken As CancellationToken) As ImmutableArray(Of TodoItem) Implements ITodoListProvider.GetTodoItems
                Assert.NotNull(workspace)
                Assert.NotNull(documentId)

                Return Items.Where(Function(t) t.DocumentId Is documentId).ToImmutableArrayOrEmpty()
            End Function

            Public Sub RaiseTodoListUpdated(workspace As Workspace)
                Dim map = Items.Where(Function(t) t.Workspace Is workspace).ToLookup(Function(t) t.DocumentId)

                For Each group In map
                    RaiseEvent TodoListUpdated(Me, New TodoListEventArgs(
                        Tuple.Create(Me, group.Key), workspace, workspace.CurrentSolution, group.Key.ProjectId, group.Key, group.ToImmutableArrayOrEmpty()))
                Next
            End Sub

            Public Sub RaiseClearTodoListUpdated(workspace As Workspace, documentId As DocumentId)
                RaiseEvent TodoListUpdated(Me, New TodoListEventArgs(
                    Tuple.Create(Me, documentId), workspace, workspace.CurrentSolution, documentId.ProjectId, documentId, ImmutableArray(Of TodoItem).Empty))
            End Sub
        End Class
    End Class
End Namespace