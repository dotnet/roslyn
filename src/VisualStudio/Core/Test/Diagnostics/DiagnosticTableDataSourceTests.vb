' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim provider = New TestDiagnosticService()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Assert.Equal(manager.Identifier, StandardTables.ErrorsTable)
                Assert.Equal(1, manager.Sources.Count())

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestDiagnosticService(CreateItem(documentId))
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Assert.Equal(1, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntryChanged()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim provider = New TestDiagnosticService()
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)

                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)

                provider.Items = New DiagnosticData() {CreateItem(documentId)}
                provider.RaiseDiagnosticsUpdated(workspace)
                Assert.Equal(1, sink.Entries.Count)

                provider.Items = Array.Empty(Of DiagnosticData)()
                provider.RaiseClearDiagnosticsUpdated(workspace, documentId.ProjectId, documentId)
                Assert.Equal(0, sink.Entries.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestDiagnosticService(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)

                Dim filename = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.DataLocation?.OriginalFilePath, filename)

                Dim text = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(If(item.DataLocation?.MappedStartLine, 0), line)

                Dim column = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(If(item.DataLocation?.MappedStartColumn, 0), column)
            End Using
        End Sub

        <Fact>
        Public Sub TestSnapshotEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestDiagnosticService(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim factory = TryCast(sink.Entries.First(), TableEntriesFactory(Of DiagnosticTableItem))
                Dim snapshot1 = factory.GetCurrentSnapshot()

                factory.OnRefreshed()

                Dim snapshot2 = factory.GetCurrentSnapshot()

                Assert.Equal(snapshot1.VersionNumber + 1, snapshot2.VersionNumber)

                Assert.Equal(1, snapshot1.Count)

                Dim filename = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))
                Assert.Equal(item.DataLocation?.OriginalFilePath, filename)

                Dim text = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Text, text))
                Assert.Equal(item.Message, text)

                Dim line = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Line, line))
                Assert.Equal(If(item.DataLocation?.MappedStartLine, 0), line)

                Dim column = Nothing
                Assert.True(snapshot1.TryGetValue(0, StandardTableKeyNames.Column, column))
                Assert.Equal(If(item.DataLocation?.MappedStartColumn, 0), column)
            End Using
        End Sub

        <Fact>
        Public Sub TestInvalidEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId)
                Dim provider = New TestDiagnosticService(item)
                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()

                Dim item = CreateItem(documentId, DiagnosticSeverity.Error)
                Dim item2 = CreateItem(documentId, DiagnosticSeverity.Hidden)
                Dim provider = New TestDiagnosticService(item, item2)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestProjectEntry()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim projectId = workspace.CurrentSolution.Projects.First().Id

                Dim item = CreateItem(projectId, Nothing, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim subscription = sinkAndSubscription.Value

                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()

                Assert.Equal(1, snapshot.Count)
            End Using
        End Sub

        <Fact>
        Public Sub TestMultipleWorkspace()
            Using workspace1 = TestWorkspace.CreateCSharp(String.Empty)
                Using workspace2 = TestWorkspace.CreateCSharp(String.Empty)
                    Dim documentId = workspace1.CurrentSolution.Projects.First().DocumentIds.First()
                    Dim projectId = documentId.ProjectId

                    Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error)
                    Dim provider = New TestDiagnosticService(item1)

                    Dim tableManagerProvider = New TestTableManagerProvider()

                    Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace1, provider, tableManagerProvider)
                    provider.RaiseDiagnosticsUpdated(workspace1)

                    Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                    Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                    Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                    Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                    Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                    Assert.Equal(1, snapshot.Count)

                    Dim item2 = CreateItem(projectId, documentId, DiagnosticSeverity.Error)
                    provider.RaiseDiagnosticsUpdated(workspace2, item2)
                    Assert.Equal(1, sink.Entries.Count)

                    Dim item3 = CreateItem(projectId, Nothing, DiagnosticSeverity.Error)
                    provider.RaiseDiagnosticsUpdated(workspace1, item3)

                    Assert.Equal(2, sink.Entries.Count)
                End Using
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDetailExpander()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error, "http://link")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim keyword As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.HelpKeyword, keyword))

                Assert.Equal(item1.Id, keyword.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub TestBingHelpLink()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error)
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim helpLink As Object = Nothing
                Assert.True(snapshot.TryGetValue(0, StandardTableKeyNames.HelpLink, helpLink))

                Assert.True(helpLink.ToString().IndexOf("https://bingdev.cloudapp.net/BingUrl.svc/Get?selectedText=test%20format&mainLanguage=C%23&projectType=%7BFAE04EC0-301F-11D3-BF4B-00C04F79EFBC%7D") = 0)
            End Using
        End Sub

        <Fact>
        Public Async Function TestBingHelpLink_NoCustomType() As Task
            Using workspace = TestWorkspace.CreateCSharp("class A { int 111a; }")
                Dim diagnostic = (Await workspace.CurrentSolution.Projects.First().GetCompilationAsync()).GetDiagnostics().First(Function(d) d.Id = "CS1519")

                Dim helpMessage = diagnostic.GetBingHelpMessage(workspace)
                Assert.Equal("Invalid token '111' in class, struct, or interface member declaration", helpMessage)

                ' turn off custom type search
                Dim optionServices = workspace.Services.GetService(Of IOptionService)()
                optionServices.SetOptions(optionServices.GetOptions().WithChangedOption(InternalDiagnosticsOptions.PutCustomTypeInBingSearch, False))

                Dim helpMessage2 = diagnostic.GetBingHelpMessage(workspace)
                Assert.Equal("Invalid token '{0}' in class, struct, or interface member declaration", helpMessage2)
            End Using
        End Function

        <Fact>
        Public Sub TestErrorSource()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, documentId, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
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
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(Nothing, Nothing, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim filename As Object = Nothing
                Assert.False(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))

                Dim projectname As Object = Nothing
                Assert.False(snapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, projectname))
            End Using
        End Sub

        <Fact>
        Public Sub TestProjectDiagnostic()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim documentId = workspace.CurrentSolution.Projects.First().DocumentIds.First()
                Dim projectId = documentId.ProjectId

                Dim item1 = CreateItem(projectId, Nothing, DiagnosticSeverity.Error, "http://link/")
                Dim provider = New TestDiagnosticService(item1)

                Dim tableManagerProvider = New TestTableManagerProvider()

                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, provider, tableManagerProvider)
                provider.RaiseDiagnosticsUpdated(workspace)

                Dim manager = DirectCast(table.TableManager, TestTableManagerProvider.TestTableManager)
                Dim source = DirectCast(manager.Sources.First(), AbstractRoslynTableDataSource(Of DiagnosticTableItem))
                Dim sinkAndSubscription = manager.Sinks_TestOnly.First()

                Dim sink = DirectCast(sinkAndSubscription.Key, TestTableManagerProvider.TestTableManager.TestSink)
                Dim snapshot = sink.Entries.First().GetCurrentSnapshot()
                Assert.Equal(1, snapshot.Count)

                Dim filename As Object = Nothing
                Assert.False(snapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, filename))

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

            Using workspace = TestWorkspace.Create(markup)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim service = New DiagnosticService(listenerProvider, Array.Empty(Of Lazy(Of IEventListener, EventListenerMetadata))())

                Dim tableManagerProvider = New TestTableManagerProvider()
                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, service, tableManagerProvider)

                Dim listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService)
                RunCompilerAnalyzer(workspace, service, listener)

                Await DirectCast(listener, IAsynchronousOperationWaiter).CreateExpeditedWaitTask()

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

            Using workspace = TestWorkspace.Create(markup)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)

                Dim listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService)
                Dim service = New DiagnosticService(listenerProvider, Array.Empty(Of Lazy(Of IEventListener, EventListenerMetadata))())
                Dim analyzerService = New MyDiagnosticAnalyzerService(ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)).Empty, service, listener)

                Dim updateSource = New ExternalErrorDiagnosticUpdateSource(workspace, analyzerService, listener)

                Dim tableManagerProvider = New TestTableManagerProvider()
                Dim table = New VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable(workspace, updateSource, tableManagerProvider)

                Dim document1 = workspace.CurrentSolution.Projects.First(Function(p) p.Name = "Proj1").Documents.First()
                Dim document2 = workspace.CurrentSolution.Projects.First(Function(p) p.Name = "Proj2").Documents.First()

                Dim diagnostic1 = CreateItem(document1.Id)
                Dim diagnostic2 = CreateItem(document2.Id)

                updateSource.AddNewErrors(
                    document1.Project.Id,
                    New DiagnosticData(
                        diagnostic1.Id,
                        diagnostic1.Category,
                        diagnostic1.Message,
                        diagnostic1.ENUMessageForBingSearch,
                        diagnostic1.Severity,
                        diagnostic1.DefaultSeverity,
                        diagnostic1.IsEnabledByDefault,
                        diagnostic1.WarningLevel,
                        diagnostic1.CustomTags,
                        diagnostic1.Properties,
                        diagnostic1.ProjectId,
                        New DiagnosticDataLocation(
                            Nothing,
                            diagnostic1.DataLocation.SourceSpan,
                            diagnostic1.DataLocation.OriginalFilePath,
                            diagnostic1.DataLocation.OriginalStartLine,
                            diagnostic1.DataLocation.OriginalStartColumn,
                            diagnostic1.DataLocation.OriginalEndLine,
                            diagnostic1.DataLocation.OriginalEndColumn,
                            diagnostic1.DataLocation.MappedFilePath,
                            diagnostic1.DataLocation.MappedStartLine,
                            diagnostic1.DataLocation.MappedStartColumn,
                            diagnostic1.DataLocation.MappedEndLine,
                            diagnostic1.DataLocation.MappedEndColumn),
                        diagnostic1.AdditionalLocations,
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
                        diagnostic2.ENUMessageForBingSearch,
                        diagnostic2.Severity,
                        diagnostic2.Severity,
                        diagnostic2.IsEnabledByDefault,
                        diagnostic2.WarningLevel,
                        diagnostic2.CustomTags,
                        diagnostic2.Properties,
                        diagnostic2.ProjectId,
                        New DiagnosticDataLocation(
                            Nothing,
                            diagnostic2.DataLocation.SourceSpan,
                            diagnostic2.DataLocation.OriginalFilePath,
                            diagnostic2.DataLocation.OriginalStartLine,
                            diagnostic2.DataLocation.OriginalStartColumn,
                            diagnostic2.DataLocation.OriginalEndLine,
                            diagnostic2.DataLocation.OriginalEndColumn,
                            diagnostic2.DataLocation.MappedFilePath,
                            diagnostic2.DataLocation.MappedStartLine,
                            diagnostic2.DataLocation.MappedStartColumn,
                            diagnostic2.DataLocation.MappedEndLine,
                            diagnostic2.DataLocation.MappedEndColumn),
                        diagnostic2.AdditionalLocations,
                        diagnostic2.Title,
                        diagnostic2.Description,
                        diagnostic2.HelpLink,
                        diagnostic2.IsSuppressed))

                updateSource.OnSolutionBuildCompleted()

                Await DirectCast(listener, IAsynchronousOperationWaiter).CreateExpeditedWaitTask()

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
        End Function

        Private Sub RunCompilerAnalyzer(workspace As TestWorkspace, registrationService As IDiagnosticUpdateSourceRegistrationService, listener As IAsynchronousOperationListener)
            Dim snapshot = workspace.CurrentSolution

            Dim compilerAnalyzersMap = DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()
            Dim analyzerService = New MyDiagnosticAnalyzerService(compilerAnalyzersMap, registrationService, listener)

            Dim service = DirectCast(workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)(), SolutionCrawlerRegistrationService)
            service.Register(workspace)

            service.WaitUntilCompletion_ForTestingPurposesOnly(workspace, SpecializedCollections.SingletonEnumerable(analyzerService.CreateIncrementalAnalyzer(workspace)).WhereNotNull().ToImmutableArray())
        End Sub

        Private Function CreateItem(documentId As DocumentId, Optional severity As DiagnosticSeverity = DiagnosticSeverity.Error) As DiagnosticData
            Return CreateItem(documentId.ProjectId, documentId, severity)
        End Function

        Private Function CreateItem(projectId As ProjectId, documentId As DocumentId, Optional severity As DiagnosticSeverity = DiagnosticSeverity.Error, Optional link As String = Nothing) As DiagnosticData
            Return New DiagnosticData(
                id:="test",
                category:="test",
                message:="test",
                enuMessageForBingSearch:="test format",
                severity:=severity,
                defaultSeverity:=severity,
                isEnabledByDefault:=True,
                warningLevel:=0,
                customTags:=ImmutableArray(Of String).Empty,
                properties:=ImmutableDictionary(Of String, String).Empty,
                projectId,
                location:=If(documentId Is Nothing, Nothing, New DiagnosticDataLocation(documentId, TextSpan.FromBounds(0, 10), "test", 20, 20, 20, 20)),
                title:="Title",
                description:="Description",
                helpLink:=link)
        End Function

        Private Class MyDiagnosticAnalyzerService
            Inherits DiagnosticAnalyzerService

            Friend Sub New(
                    analyzersMap As ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)),
                    registrationService As IDiagnosticUpdateSourceRegistrationService,
                    listener As IAsynchronousOperationListener)
                MyBase.New(New HostAnalyzerManager(ImmutableArray.Create(Of AnalyzerReference)(New TestAnalyzerReferenceByLanguage(analyzersMap)), hostDiagnosticUpdateSource:=Nothing),
                      hostDiagnosticUpdateSource:=Nothing,
                      registrationService:=registrationService,
                      listener:=listener)
            End Sub
        End Class

        Private Class TestDiagnosticService
            Implements IDiagnosticService

            Public Items As DiagnosticData()

            Public Sub New(ParamArray items As DiagnosticData())
                Me.Items = items
            End Sub

            Public Event DiagnosticsUpdated As EventHandler(Of DiagnosticsUpdatedArgs) Implements IDiagnosticService.DiagnosticsUpdated

            Public Function GetDiagnostics(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, reportSuppressedDiagnostics As Boolean, cancellationToken As CancellationToken) As IEnumerable(Of DiagnosticData) Implements IDiagnosticService.GetDiagnostics
                Assert.NotNull(workspace)

                Dim diagnostics As IEnumerable(Of DiagnosticData)

                If documentId IsNot Nothing Then
                    diagnostics = Items.Where(Function(t) t.DocumentId Is documentId).ToImmutableArrayOrEmpty()
                ElseIf projectId IsNot Nothing Then
                    diagnostics = Items.Where(Function(t) t.ProjectId Is projectId).ToImmutableArrayOrEmpty()
                Else
                    diagnostics = Items.ToImmutableArrayOrEmpty()
                End If

                If Not reportSuppressedDiagnostics Then
                    diagnostics = diagnostics.Where(Function(d) Not d.IsSuppressed)
                End If

                Return diagnostics
            End Function

            Public Function GetDiagnosticsArgs(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, cancellationToken As CancellationToken) As IEnumerable(Of UpdatedEventArgs) Implements IDiagnosticService.GetDiagnosticsUpdatedEventArgs
                Assert.NotNull(workspace)

                Dim diagnosticsArgs As IEnumerable(Of UpdatedEventArgs)

                If documentId IsNot Nothing Then
                    diagnosticsArgs = Items.Where(Function(t) t.DocumentId Is documentId) _
                                           .Select(
                                                Function(t)
                                                    Return New UpdatedEventArgs(
                                                        New ErrorId(Me, If(CObj(t.DocumentId), t.ProjectId)),
                                                        workspace, t.ProjectId, t.DocumentId, "BuildTool")
                                                End Function).ToImmutableArrayOrEmpty()
                ElseIf projectId IsNot Nothing Then
                    diagnosticsArgs = Items.Where(Function(t) t.ProjectId Is projectId) _
                                           .Select(
                                                Function(t)
                                                    Return New UpdatedEventArgs(
                                                        New ErrorId(Me, If(CObj(t.DocumentId), t.ProjectId)),
                                                        workspace, t.ProjectId, t.DocumentId, "BuildTool")
                                                End Function).ToImmutableArrayOrEmpty()
                Else
                    diagnosticsArgs = Items.Select(
                                                Function(t)
                                                    Return New UpdatedEventArgs(
                                                        New ErrorId(Me, If(CObj(t.DocumentId), t.ProjectId)),
                                                        workspace, t.ProjectId, t.DocumentId, "BuildTool")
                                                End Function).ToImmutableArrayOrEmpty()
                End If

                Return diagnosticsArgs
            End Function

            Public Sub RaiseDiagnosticsUpdated(workspace As Workspace, ParamArray items As DiagnosticData())
                Dim item = items(0)

                Dim id = If(CObj(item.DocumentId), item.ProjectId)
                RaiseEvent DiagnosticsUpdated(Me, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        New ErrorId(Me, id), workspace, workspace.CurrentSolution, item.ProjectId, item.DocumentId, buildTool:="BuildTool", items.ToImmutableArray()))
            End Sub

            Public Sub RaiseDiagnosticsUpdated(workspace As Workspace)
                Dim documentMap = Items.Where(Function(t) t.DocumentId IsNot Nothing).ToLookup(Function(t) t.DocumentId)

                For Each group In documentMap
                    RaiseEvent DiagnosticsUpdated(Me, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        New ErrorId(Me, group.Key), workspace, workspace.CurrentSolution, group.Key.ProjectId, group.Key, buildTool:="BuildTool", group.ToImmutableArrayOrEmpty()))
                Next

                Dim projectMap = Items.Where(Function(t) t.DocumentId Is Nothing).ToLookup(Function(t) t.ProjectId)

                For Each group In projectMap
                    RaiseEvent DiagnosticsUpdated(Me, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        New ErrorId(Me, group.Key), workspace, workspace.CurrentSolution, group.Key, documentId:=Nothing, buildTool:="BuildTool", group.ToImmutableArrayOrEmpty()))
                Next
            End Sub

            Public Sub RaiseClearDiagnosticsUpdated(workspace As Workspace, projectId As ProjectId, documentId As DocumentId)
                RaiseEvent DiagnosticsUpdated(Me, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                    New ErrorId(Me, documentId), workspace, workspace.CurrentSolution, projectId, documentId, buildTool:="BuildTool"))
            End Sub

            Private Class ErrorId
                Inherits BuildToolId.Base(Of TestDiagnosticService, Object)

                Public Sub New(service As TestDiagnosticService, id As Object)
                    MyBase.New(service, id)
                End Sub
            End Class
        End Class
    End Class
End Namespace
