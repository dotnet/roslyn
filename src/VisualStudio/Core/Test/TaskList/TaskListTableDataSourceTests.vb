' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.TaskList
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.TaskList
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
Imports Microsoft.VisualStudio.Shell.TableManager
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.TaskList
    <[UseExportProvider]>
    Public Class TaskListTableDataSourceTests
        <Fact>
        Public Sub TestCreation()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim provider = New TestTaskListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Assert.Equal(manager.Identifier, StandardTables.TasksTable)
                Assert.Equal(1, manager.Sources.Count())

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
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
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestTaskListProvider(CreateItem(documentId))
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Assert.Equal(1, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntryChanged()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestTaskListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)

                provider.Items = New TaskListItem() {CreateItem(documentId)}
                provider.RaiseTodoListUpdated(workspace)
                Assert.Equal(1, sink.Entries.Count)

                provider.Items = Array.Empty(Of TaskListItem)()
                provider.RaiseClearTodoListUpdated(workspace, documentId)
                Assert.Equal(0, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTaskListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
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
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTaskListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TaskListTableItem, TaskListUpdatedArgs))
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
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTaskListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(0, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo2()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTaskListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                Dim pos = New LinePosition(11, 21)
                Dim span2 = New FileLinePositionSpan("test2", pos, pos)
                Dim span1 = New FileLinePositionSpan("test1", pos, pos)
                provider.Items = New TaskListItem() {
                    New TaskListItem(Priority:=TaskListItemPriority.Medium, Message:="test2", DocumentId:=documentId, Span:=span2, MappedSpan:=span2),
                    New TaskListItem(Priority:=TaskListItemPriority.Low, Message:="test", DocumentId:=documentId, Span:=span1, MappedSpan:=span1)
                }

                provider.RaiseTodoListUpdated(workspace)

                Dim snapshot2 = factory.GetCurrentSnapshot()
                Assert.Equal(1, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotTranslateTo3()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTaskListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of TaskListTableItem, TaskListUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                Dim pos = New LinePosition(11, 21)
                Dim span2 = New FileLinePositionSpan("test2", pos, pos)
                Dim span3 = New FileLinePositionSpan("test3", pos, pos)
                provider.Items = New TaskListItem() {
                    New TaskListItem(Priority:=TaskListItemPriority.Medium, Message:="test2", DocumentId:=documentId, Span:=span2, MappedSpan:=span2),
                    New TaskListItem(Priority:=TaskListItemPriority.Low, Message:="test3", DocumentId:=documentId, Span:=span3, MappedSpan:=span3)
                }

                provider.RaiseTodoListUpdated(workspace)

                Dim snapshot2 = factory.GetCurrentSnapshot()
                Assert.Equal(-1, snapshot1.IndexOf(0, snapshot2))
            End Using
        End Sub

        <Fact>
        Public Sub TestInvalidEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestTaskListProvider(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
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

            Using workspace = EditorTestWorkspace.Create(markup)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim projects = workspace.CurrentSolution.Projects.ToArray()

                Dim item1 = CreateItem(projects(0).DocumentIds.First())
                Dim item2 = CreateItem(projects(1).DocumentIds.First())

                Dim provider = New TestTaskListProvider()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioTaskListTable(workspace, threadingContext, tableManagerProvider, provider)

                provider.Items = New TaskListItem() {item1, item2}
                provider.RaiseTodoListUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of TaskListTableItem, TaskListUpdatedArgs))
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

        Private Shared Function CreateItem(documentId As DocumentId) As TaskListItem
            Dim pos = New LinePosition(10, 20)
            Dim span = New FileLinePositionSpan("test1", pos, pos)
            Return New TaskListItem(
                priority:=0,
                message:="test",
                documentId:=documentId,
                span:=span,
                mappedSpan:=span)
        End Function

        Private Class TestTaskListProvider
            Implements ITaskListProvider

            Public Items As TaskListItem()

            Public Sub New(ParamArray items As TaskListItem())
                Me.Items = items
            End Sub

            Public Event TaskListUpdated As EventHandler(Of TaskListUpdatedArgs) Implements ITaskListProvider.TaskListUpdated

            Public Function GetTaskListItems(workspace As Workspace, documentId As DocumentId, cancellationToken As CancellationToken) As ImmutableArray(Of TaskListItem) Implements ITaskListProvider.GetTaskListItems
                Assert.NotNull(workspace)
                Assert.NotNull(documentId)

                Return Items.Where(Function(t) t.DocumentId Is documentId).ToImmutableArrayOrEmpty()
            End Function

            Public Sub RaiseTodoListUpdated(workspace As Workspace)
                Dim map = Items.ToLookup(Function(t) t.DocumentId)

                For Each group In map
                    RaiseEvent TaskListUpdated(Me, New TaskListUpdatedArgs(
                        Me, workspace.CurrentSolution, group.Key, group.ToImmutableArrayOrEmpty()))
                Next
            End Sub

            Public Sub RaiseClearTodoListUpdated(workspace As Microsoft.CodeAnalysis.Workspace, documentId As DocumentId)
                RaiseEvent TaskListUpdated(Me, New TaskListUpdatedArgs(
                    Me, workspace.CurrentSolution, documentId, ImmutableArray(Of TaskListItem).Empty))
            End Sub
        End Class
    End Class
End Namespace
