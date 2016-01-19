' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class ExternalDiagnosticUpdateSourceTests
        <Fact>
        Public Async Function TestExternalDiagnostics_SupportGetDiagnostics() As Task
            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(String.Empty)
                Dim waiter = New Waiter()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, New MockDiagnosticUpdateSourceRegistrationService(), waiter)

                Assert.False(source.SupportGetDiagnostics)
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_RaiseEvents() As Task
            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(String.Empty)
                Dim waiter = New Waiter()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, New MockDiagnosticUpdateSourceRegistrationService(), waiter)

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(workspace, project.Id)

                Dim expected = 1
                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(expected, a.Diagnostics.Length)
                                                          If expected = 1 Then
                                                              Assert.Equal(a.Diagnostics(0), diagnostic)
                                                          End If
                                                      End Sub

                source.AddNewErrors(project.DocumentIds.First(), diagnostic)
                source.OnSolutionBuild(Me, Shell.UIContextChangedEventArgs.From(False))
                Await waiter.CreateWaitTask()

                expected = 0
                source.ClearErrors(project.Id)
                Await waiter.CreateWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_DuplicatedError() As Task
            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(String.Empty)
                Dim waiter = New Waiter()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(workspace, project.Id)

                Dim service = New TestDiagnosticAnalyzerService(ImmutableArray.Create(Of DiagnosticData)(diagnostic))
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, New MockDiagnosticUpdateSourceRegistrationService(), waiter)

                Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(workspace, project.Id))))

                source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                source.OnSolutionBuild(Me, Shell.UIContextChangedEventArgs.From(False))

                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(1, a.Diagnostics.Length)
                                                      End Sub
                Await waiter.CreateWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestBuildStartEvent() As Task
            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(String.Empty)
                Dim waiter = New Waiter()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(workspace, project.Id)

                Dim service = New TestDiagnosticAnalyzerService(ImmutableArray(Of DiagnosticData).Empty)
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, New MockDiagnosticUpdateSourceRegistrationService(), waiter)
                AddHandler source.BuildStarted, Sub(o, started)
                                                    If Not started Then
                                                        Assert.Equal(2, source.GetBuildErrors().Length)
                                                    End If
                                                End Sub

                Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(workspace, project.Id))))

                source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                Await waiter.CreateWaitTask()

                source.OnSolutionBuild(Me, Shell.UIContextChangedEventArgs.From(False))
                Await waiter.CreateWaitTask()
            End Using
        End Function

        <Fact>
        Public Sub TestExternalBuildErrorCustomTags()
            Assert.Equal(1, ProjectExternalErrorReporter.CustomTags.Count)
            Assert.Equal(WellKnownDiagnosticTags.Telemetry, ProjectExternalErrorReporter.CustomTags(0))
        End Sub

        <Fact>
        Public Async Function TestExternalBuildErrorProperties() As Task
            Assert.Equal(1, DiagnosticData.PropertiesForBuildDiagnostic.Count)

            Dim value As String = Nothing
            Assert.True(DiagnosticData.PropertiesForBuildDiagnostic.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, value))
            Assert.Equal(WellKnownDiagnosticTags.Build, value)

            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(String.Empty)
                Dim project = workspace.CurrentSolution.Projects.First()

                Dim diagnostic = GetDiagnosticData(workspace, project.Id, isBuildDiagnostic:=True)
                Assert.True(diagnostic.IsBuildDiagnostic())

                diagnostic = GetDiagnosticData(workspace, project.Id, isBuildDiagnostic:=False)
                Assert.False(diagnostic.IsBuildDiagnostic())
            End Using
        End Function

        Private Function GetDiagnosticData(workspace As Workspace, projectId As ProjectId, Optional isBuildDiagnostic As Boolean = False) As DiagnosticData
            Dim properties = If(isBuildDiagnostic, DiagnosticData.PropertiesForBuildDiagnostic, ImmutableDictionary(Of String, String).Empty)
            Return New DiagnosticData(
                "Id", "Test", "Test Message", "Test Message Format", DiagnosticSeverity.Error, True, 0, workspace, projectId, properties:=properties)
        End Function

        Private Class Waiter
            Inherits AsynchronousOperationListener
        End Class

        Private Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService, IDiagnosticUpdateSource

            Private ReadOnly _data As ImmutableArray(Of DiagnosticData)

            Public Sub New()
            End Sub

            Public Sub New(data As ImmutableArray(Of DiagnosticData))
                Me._data = data
            End Sub

            Public ReadOnly Property SupportGetDiagnostics As Boolean Implements IDiagnosticUpdateSource.SupportGetDiagnostics
                Get
                    Return True
                End Get
            End Property

            Public Event DiagnosticsUpdated As EventHandler(Of DiagnosticsUpdatedArgs) Implements IDiagnosticUpdateSource.DiagnosticsUpdated

            Public Function GetDiagnostics(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, includeSuppressedDiagnostics As Boolean, cancellationToken As CancellationToken) As ImmutableArray(Of DiagnosticData) Implements IDiagnosticUpdateSource.GetDiagnostics
                Return If(includeSuppressedDiagnostics, _data, _data.WhereAsArray(Function(d) Not d.IsSuppressed))
            End Function

            Public Sub Reanalyze(workspace As Workspace, Optional projectIds As IEnumerable(Of ProjectId) = Nothing, Optional documentIds As IEnumerable(Of DocumentId) = Nothing, Optional highPriority As Boolean = False) Implements IDiagnosticAnalyzerService.Reanalyze
            End Sub

            Public Function GetDiagnosticDescriptors(projectOpt As Project) As ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)) Implements IDiagnosticAnalyzerService.GetDiagnosticDescriptors
                Return ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)).Empty
            End Function

            Public Function GetDiagnosticsForSpanAsync(document As Document, range As TextSpan, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync
                Return Task.FromResult(SpecializedCollections.EmptyEnumerable(Of DiagnosticData))
            End Function

            Public Function TryAppendDiagnosticsForSpanAsync(document As Document, range As TextSpan, diagnostics As List(Of DiagnosticData), Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDiagnosticAnalyzerService.TryAppendDiagnosticsForSpanAsync
                Return Task.FromResult(False)
            End Function

            Public Function GetSpecificCachedDiagnosticsAsync(workspace As Workspace, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetCachedDiagnosticsAsync(workspace As Workspace, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetSpecificDiagnosticsAsync(solution As Solution, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetProjectDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticDescriptors(analyzer As DiagnosticAnalyzer) As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzerService.GetDiagnosticDescriptors
                Return ImmutableArray(Of DiagnosticDescriptor).Empty
            End Function

            Public Function IsCompilerDiagnostic(language As String, diagnostic As DiagnosticData) As Boolean Implements IDiagnosticAnalyzerService.IsCompilerDiagnostic
                Return False
            End Function

            Public Function GetCompilerDiagnosticAnalyzer(language As String) As DiagnosticAnalyzer Implements IDiagnosticAnalyzerService.GetCompilerDiagnosticAnalyzer
                Return Nothing
            End Function

            Public Function IsCompilerDiagnosticAnalyzer(language As String, analyzer As DiagnosticAnalyzer) As Boolean Implements IDiagnosticAnalyzerService.IsCompilerDiagnosticAnalyzer
                Return False
            End Function
        End Class
    End Class
End Namespace
