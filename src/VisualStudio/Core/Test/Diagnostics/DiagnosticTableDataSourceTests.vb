' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Shared
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.Shell.TableControl
Imports Microsoft.VisualStudio.Shell.TableManager
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <[UseExportProvider]>
    Public Class DiagnosticTableDataSourceTests
        <Fact>
        Public Sub TestCreation()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim provider = New TestDiagnosticService()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Assert.Equal(manager.Identifier, StandardTables.ErrorsTable)
                Assert.Equal(1, manager.Sources.Count())

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                AssertEx.SetEqual(table.Columns, manager.GetColumnsForSources(SpecializedCollections.SingletonEnumerable(source)))

                Assert.Equal(ServicesVSResources.CSharp_VB_Diagnostics_Table_Data_Source, source.DisplayName)
                Assert.Equal(StandardTableDataSources.ErrorTableDataSource, source.SourceTypeIdentifier)

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
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestDiagnosticService(CreateItem(workspace.CurrentSolution, documentId))
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Assert.Equal(1, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntryChanged()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestDiagnosticService()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)

                provider.Items = New DiagnosticData() {CreateItem(workspace.CurrentSolution, documentId)}
                provider.RaiseDiagnosticsUpdated(workspace)
                Assert.Equal(1, sink.Entries.Count)

                provider.Items = Array.Empty(Of DiagnosticData)()
                provider.RaiseClearDiagnosticsUpdated(workspace, documentId.ProjectId, documentId)
                Assert.Equal(0, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace.CurrentSolution, documentId)
                Dim provider = New TestDiagnosticService(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)

                Dim filename = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.DataLocation.UnmappedFileSpan.Path, filename)

                Dim text = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(item.DataLocation.MappedFileSpan.StartLinePosition.Line, line)

                Dim column = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(item.DataLocation.MappedFileSpan.StartLinePosition.Character, column)
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace.CurrentSolution, documentId)
                Dim provider = New TestDiagnosticService(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(snapshot1.VersionNumber + 1, snapshot2.VersionNumber)

                Assert.Equal(1, snapshot1.Count)

                Dim filename = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.DataLocation.UnmappedFileSpan.Path, filename)

                Dim text = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(item.DataLocation.MappedFileSpan.StartLinePosition.Line, line)

                Dim column = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(item.DataLocation.MappedFileSpan.StartLinePosition.Character, column)
            End Using
        End Sub

        <Fact>
        Public Sub TestInvalidEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace.CurrentSolution, documentId)
                Dim provider = New TestDiagnosticService(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
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

        <Fact>
        Public Sub TestNoHiddenEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(workspace.CurrentSolution, documentId, DiagnosticSeverity.Error)
                Dim item2 = CreateItem(workspace.CurrentSolution, documentId, DiagnosticSeverity.Hidden)
                Dim provider = New TestDiagnosticService(item, item2)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestProjectEntry()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim projectId = workspace.CurrentSolution.Projects.First().Id

                Dim item = CreateItem(workspace.CurrentSolution, projectId, Nothing, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestMultipleWorkspace()
            Using workspace1 = EditorTestWorkspace.CreateCSharp(String.Empty)
                Using workspace2 = EditorTestWorkspace.CreateCSharp(String.Empty)
                    Dim documentId = workspace1.CurrentSolution.Projects.First().DocumentIds.First()
                    Dim projectId = documentId.ProjectId

                    Dim item1 = CreateItem(workspace1.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error)
                    Dim globalOptions = workspace1.GetService(Of IGlobalOptionService)()
                    Dim provider = New TestDiagnosticService(item1)

                    Dim tableManagerProvider = New TestTableManagerProvider()

                    Dim threadingContext = workspace1.GetService(Of IThreadingContext)()
                    Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace1, globalOptions, threadingContext, provider, tableManagerProvider)
                    provider.RaiseDiagnosticsUpdated(workspace1)

                    Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                    Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                    Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                    Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                    Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                    Assert.Equal(1, snapshot.Count)

                    Dim item2 = CreateItem(workspace2.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error)
                    provider.RaiseDiagnosticsUpdated(workspace2, item2)
                    Assert.Equal(1, sink.Entries.Count)

                    Dim item3 = CreateItem(workspace1.CurrentSolution, projectId, Nothing, DiagnosticSeverity.Error)
                    provider.RaiseDiagnosticsUpdated(workspace1, item3)

                    Assert.Equal(2, sink.Entries.Count)
                End Using
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDetailExpander()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim wpfTableEntriesSnapshot = TryCast(snapshot, IWpfTableEntriesSnapshot)
                Assert.NotNull(wpfTableEntriesSnapshot)

                Assert.True(wpfTableEntriesSnapshot.CanCreateDetailsContent(0))

                Dim ui As FrameworkElement = Nothing
                Assert.True(wpfTableEntriesSnapshot.TryCreateDetailsContent(0, ui))

                Dim textBlock = TryCast(ui, TextBlock)
                Assert.Equal(item1.Description, textBlock.Text)
                Assert.Equal(New Thickness(10, 6, 10, 8), textBlock.Padding)
                Assert.Equal(Nothing, textBlock.Background)
            End Using
        End Sub

        <Fact>
        Public Sub TestHyperLink()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error, "http://link")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim wpfTableEntriesSnapshot = TryCast(snapshot, IWpfTableEntriesSnapshot)
                Assert.NotNull(wpfTableEntriesSnapshot)

                Dim ui As FrameworkElement = Nothing
                Assert.False(wpfTableEntriesSnapshot.TryCreateColumnContent(0, StandardTableKeyNames.ErrorCode, False, ui))

                Assert.Null(ui)
            End Using
        End Sub

        <Fact>
        Public Sub TestBingHyperLink()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim wpfTableEntriesSnapshot = TryCast(snapshot, IWpfTableEntriesSnapshot)
                Assert.NotNull(wpfTableEntriesSnapshot)

                Dim ui As FrameworkElement = Nothing
                Assert.False(wpfTableEntriesSnapshot.TryCreateColumnContent(0, StandardTableKeyNames.ErrorCode, False, ui))

                Assert.Null(ui)
            End Using
        End Sub

        <Fact>
        Public Sub TestHelpLink()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim helpLink As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.HelpLink, helpLink))

                Assert.Equal(item1.HelpLink, helpLink.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub TestHelpKeyword()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error, "http://link/")
                Dim item2 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error, customTags:={WellKnownDiagnosticCustomTags.DoesNotSupportF1Help})
                Dim provider = New TestDiagnosticService(item1, item2)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(2, snapshot.Count)

                Dim keyword As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.HelpKeyword, keyword))
                Assert.Equal(item1.Id, keyword.ToString())

                Assert.False(snapshot.TryGetValue(1, StandardTableKeyNames.HelpKeyword, keyword))
            End Using
        End Sub

        <Fact>
        Public Sub TestErrorSource()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, documentId, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim buildTool As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.BuildTool, buildTool))

                Assert.Equal("BuildTool", buildTool.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub TestWorkspaceDiagnostic()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, Nothing, Nothing, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim filename As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))

                Dim projectname As Object = Nothing
                Assert.False(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, projectname))
            End Using
        End Sub

        <Fact>
        Public Sub TestProjectDiagnostic()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(workspace.CurrentSolution, projectId, Nothing, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem, DiagnosticsUpdatedArgs))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim filename As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(workspace.CurrentSolution.GetProject(projectId).FilePath, filename)

                Dim projectname As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, projectname))

                Assert.Equal("Test", projectname)
            End Using
        End Sub

        <WpfFact>
        Public Async Function TestAggregatedDiagnostic() As Task
            Dim markup = <Workspace>
                             <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                                 <Document FilePath="CurrentDocument.cs"><![CDATA[class { }]]></Document>
                             </Project>
                             <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                             </Project>
                         </Workspace>

            Using workspace = EditorTestWorkspace.Create(markup)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim service = Assert.IsType(Of DiagnosticService)(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService)())

                Dim tableManagerProvider = New TestTableManagerProvider()
                Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, globalOptions, threadingContext, service, tableManagerProvider)

                RunCompilerAnalyzer(workspace)

                Dim analyzerService = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.ExportProvider.GetExportedValue(Of IDiagnosticAnalyzerService)())
                Await DirectCast(analyzerService.Listener, IAsynchronousOperationWaiter).ExpeditedWaitAsync()

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim filename As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal("CurrentDocument.cs", filename)

                Dim projectname As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, projectname))
                Assert.Equal("Proj1, Proj2", projectname)

                Dim projectnames As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName + "s", projectnames))
                Assert.Equal(2, DirectCast(projectnames, String()).Length)

                Dim projectguid As Object = Nothing
                Assert.False(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectGuid, projectguid))
            End Using
        End Function

        <WpfFact>
        Public Async Function TestAggregatedDiagnosticCSErrorWithFileLocationButNoDocumentId() As Task
            Dim markup = <Workspace>
                             <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                                 <Document FilePath="CurrentDocument.cs"><![CDATA[class { }]]></Document>
                             </Project>
                             <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                             </Project>
                         </Workspace>

            Using workspace = EditorTestWorkspace.Create(markup)
                Dim threadingContext = workspace.GetService(Of IThreadingContext)()
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                Dim notificationService = workspace.GetService(Of IGlobalOperationNotificationService)
                Dim listenerProvider = workspace.GetService(Of IAsynchronousOperationListenerProvider)
                Dim listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService)
                Dim service = Assert.IsType(Of DiagnosticService)(workspace.GetService(Of IDiagnosticService)())
                Dim analyzerService = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())

                Using updateSource = New ExternalErrorDiagnosticUpdateSource(workspace, analyzerService, notificationService, listener, CancellationToken.None)

                    Dim tableManagerProvider = New TestTableManagerProvider()
                    Dim table = VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.TestAccessor.Create(workspace, threadingContext, updateSource, tableManagerProvider)

                    Dim document1 = workspace.CurrentSolution.Projects.First(Function(p) p.Name = "Proj1").Documents.First()
                    Dim document2 = workspace.CurrentSolution.Projects.First(Function(p) p.Name = "Proj2").Documents.First()

                    Dim diagnostic1 = CreateItem(workspace.CurrentSolution, document1.Id)
                    Dim diagnostic2 = CreateItem(workspace.CurrentSolution, document2.Id)

                    updateSource.AddNewErrors(
                        document1.Project.Id,
                        New DiagnosticData(
                            diagnostic1.Id,
                            diagnostic1.Category,
                            diagnostic1.Message,
                            diagnostic1.Severity,
                            diagnostic1.DefaultSeverity,
                            diagnostic1.IsEnabledByDefault,
                            diagnostic1.WarningLevel,
                            diagnostic1.CustomTags,
                            diagnostic1.Properties.AddRange(DiagnosticData.PropertiesForBuildDiagnostic),
                            diagnostic1.ProjectId,
                            New DiagnosticDataLocation(
                                diagnostic1.DataLocation.UnmappedFileSpan,
                                Nothing,
                                diagnostic1.DataLocation.MappedFileSpan),
                            diagnostic1.AdditionalLocations,
                            diagnostic1.Language,
                            diagnostic1.Title,
                            diagnostic1.Description,
                            diagnostic1.HelpLink,
                            diagnostic1.IsSuppressed))

                    updateSource.AddNewErrors(
                        document2.Project.Id,
                        New DiagnosticData(
                            diagnostic2.Id,
                            diagnostic2.Category,
                            diagnostic2.Message,
                            diagnostic2.Severity,
                            diagnostic2.Severity,
                            diagnostic2.IsEnabledByDefault,
                            diagnostic2.WarningLevel,
                            diagnostic2.CustomTags,
                            diagnostic2.Properties.AddRange(DiagnosticData.PropertiesForBuildDiagnostic),
                            diagnostic2.ProjectId,
                            New DiagnosticDataLocation(
                                diagnostic2.DataLocation.UnmappedFileSpan,
                                Nothing,
                                diagnostic2.DataLocation.MappedFileSpan),
                            diagnostic2.AdditionalLocations,
                            diagnostic2.Language,
                            diagnostic2.Title,
                            diagnostic2.Description,
                            diagnostic2.HelpLink,
                            diagnostic2.IsSuppressed))

                    updateSource.OnSolutionBuildCompleted()

                    Await DirectCast(listener, IAsynchronousOperationWaiter).ExpeditedWaitAsync()

                    Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                    Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                    Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                    Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                    Assert.Equal(2, snapshot.Count)

                    Dim filename As Object = Nothing
                    Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                    Assert.Equal("test", filename)

                    Dim projectname As Object = Nothing
                    Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, projectname))
                    Assert.Equal("Proj1", projectname)
                End Using
            End Using
        End Function

        Private Shared Sub RunCompilerAnalyzer(workspace As EditorTestWorkspace)
            Dim snapshot = workspace.CurrentSolution

            Dim analyzerService = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.ExportProvider.GetExportedValue(Of IDiagnosticAnalyzerService)())

            Dim service = DirectCast(workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)(), SolutionCrawlerRegistrationService)
            service.Register(workspace)

            service.GetTestAccessor().WaitUntilCompletion(workspace, SpecializedCollections.SingletonEnumerable(analyzerService.CreateIncrementalAnalyzer(workspace)).WhereNotNull().ToImmutableArray())
        End Sub

        Private Shared Function CreateItem(solution As Solution, documentId As DocumentId, Optional severity As DiagnosticSeverity = DiagnosticSeverity.Error) As DiagnosticData
            Return CreateItem(solution, documentId.ProjectId, documentId, severity)
        End Function

        Private Shared Function CreateItem(solution As Solution, projectId As ProjectId, documentId As DocumentId, Optional severity As DiagnosticSeverity = DiagnosticSeverity.Error, Optional link As String = Nothing, Optional customTags As String() = Nothing) As DiagnosticData
            Dim location =
                If(documentId Is Nothing,
                    If(projectId Is Nothing, New DiagnosticDataLocation(New FileLinePositionSpan("", Nothing)), New DiagnosticDataLocation(New FileLinePositionSpan(solution.GetProject(projectId).FilePath, Nothing))),
                    New DiagnosticDataLocation(New FileLinePositionSpan("test", New LinePosition(20, 20), New LinePosition(20, 20)), documentId))

            Return New DiagnosticData(
                id:="test",
                category:="test",
                message:="test",
                severity:=severity,
                defaultSeverity:=severity,
                isEnabledByDefault:=True,
                warningLevel:=0,
                customTags:=If(customTags IsNot Nothing, customTags.ToImmutableArray(), ImmutableArray(Of String).Empty),
                properties:=ImmutableDictionary(Of String, String).Empty,
                projectId,
                location:=location,
                language:=LanguageNames.VisualBasic,
                title:="Title",
                description:="Description",
                helpLink:=link)
        End Function

        Private Class TestDiagnosticService
            Implements IDiagnosticService

            Public Items As DiagnosticData()

            Public Sub New(ParamArray items As DiagnosticData())
                Me.Items = items
            End Sub

            Public Event DiagnosticsUpdated As EventHandler(Of ImmutableArray(Of DiagnosticsUpdatedArgs)) Implements IDiagnosticService.DiagnosticsUpdated

            Public Function GetDiagnosticsAsync(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, includeSuppressedDiagnostics As Boolean, cancellationToken As CancellationToken) As ValueTask(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticService.GetDiagnosticsAsync
                Return New ValueTask(Of ImmutableArray(Of DiagnosticData))(GetDiagnostics(workspace, projectId, documentId, includeSuppressedDiagnostics))
            End Function

            Private Function GetDiagnostics(
                    workspace As Workspace,
                    projectId As ProjectId,
                    documentId As DocumentId,
                    includeSuppressedDiagnostics As Boolean) As ImmutableArray(Of DiagnosticData)
                Assert.NotNull(workspace)

                Dim diagnostics As ImmutableArray(Of DiagnosticData)

                If documentId IsNot Nothing Then
                    diagnostics = Items.Where(Function(t) t.DocumentId Is documentId).ToImmutableArrayOrEmpty()
                ElseIf projectId IsNot Nothing Then
                    diagnostics = Items.Where(Function(t) t.ProjectId Is projectId).ToImmutableArrayOrEmpty()
                Else
                    diagnostics = Items.ToImmutableArrayOrEmpty()
                End If

                If Not includeSuppressedDiagnostics Then
                    diagnostics = diagnostics.WhereAsArray(Function(d) Not d.IsSuppressed)
                End If

                Return diagnostics
            End Function

            Public Function GetDiagnosticBuckets(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, cancellationToken As CancellationToken) As ImmutableArray(Of DiagnosticBucket) Implements IDiagnosticService.GetDiagnosticBuckets
                Return GetDiagnosticsBuckets(workspace, projectId, documentId)
            End Function

            Private Function GetDiagnosticsBuckets(
                    workspace As Workspace,
                    projectId As ProjectId,
                    documentId As DocumentId) As ImmutableArray(Of DiagnosticBucket)
                Assert.NotNull(workspace)

                Dim diagnosticsArgs As ImmutableArray(Of DiagnosticBucket)

                If documentId IsNot Nothing Then
                    diagnosticsArgs = Items.Where(Function(t) t.DocumentId Is documentId) _
                                           .Select(
                                                Function(t)
                                                    Return New DiagnosticBucket(
                                                        New ErrorId(Me, If(CObj(t.DocumentId), t.ProjectId)),
                                                        workspace, t.ProjectId, t.DocumentId)
                                                End Function).ToImmutableArrayOrEmpty()
                ElseIf projectId IsNot Nothing Then
                    diagnosticsArgs = Items.Where(Function(t) t.ProjectId Is projectId) _
                                           .Select(
                                                Function(t)
                                                    Return New DiagnosticBucket(
                                                        New ErrorId(Me, If(CObj(t.DocumentId), t.ProjectId)),
                                                        workspace, t.ProjectId, t.DocumentId)
                                                End Function).ToImmutableArrayOrEmpty()
                Else
                    diagnosticsArgs = Items.Select(
                                                Function(t)
                                                    Return New DiagnosticBucket(
                                                        New ErrorId(Me, If(CObj(t.DocumentId), t.ProjectId)),
                                                        workspace, t.ProjectId, t.DocumentId)
                                                End Function).ToImmutableArrayOrEmpty()
                End If

                Return diagnosticsArgs
            End Function

            Public Sub RaiseDiagnosticsUpdated(workspace As Workspace, ParamArray items As DiagnosticData())
                Dim item = items(0)

                Dim id = If(CObj(item.DocumentId), item.ProjectId)
                RaiseEvent DiagnosticsUpdated(Me, ImmutableArray.Create(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        New ErrorId(Me, id), workspace, workspace.CurrentSolution, item.ProjectId, item.DocumentId, items.ToImmutableArray())))
            End Sub

            Public Sub RaiseDiagnosticsUpdated(workspace As Workspace)
                Dim documentMap = Items.Where(Function(t) t.DocumentId IsNot Nothing).ToLookup(Function(t) t.DocumentId)

                For Each group In documentMap
                    RaiseEvent DiagnosticsUpdated(Me, ImmutableArray.Create(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        New ErrorId(Me, group.Key), workspace, workspace.CurrentSolution, group.Key.ProjectId, group.Key, group.ToImmutableArrayOrEmpty())))
                Next

                Dim projectMap = Items.Where(Function(t) t.DocumentId Is Nothing).ToLookup(Function(t) t.ProjectId)

                For Each group In projectMap
                    RaiseEvent DiagnosticsUpdated(Me, ImmutableArray.Create(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        New ErrorId(Me, group.Key), workspace, workspace.CurrentSolution, group.Key, Nothing, group.ToImmutableArrayOrEmpty())))
                Next
            End Sub

            Public Sub RaiseClearDiagnosticsUpdated(workspace As Microsoft.CodeAnalysis.Workspace, projectId As ProjectId, documentId As DocumentId)
                RaiseEvent DiagnosticsUpdated(Me, ImmutableArray.Create(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                    New ErrorId(Me, documentId), workspace, workspace.CurrentSolution, projectId, documentId)))
            End Sub

            Private Class ErrorId
                Inherits BuildToolId.Base(Of TestDiagnosticService, Object)

                Public Sub New(service As TestDiagnosticService, id As Object)
                    MyBase.New(service, id)
                End Sub

                Public Overrides ReadOnly Property BuildTool As String
                    Get
                        Return "BuildTool"
                    End Get
                End Property
            End Class
        End Class
    End Class
End Namespace
