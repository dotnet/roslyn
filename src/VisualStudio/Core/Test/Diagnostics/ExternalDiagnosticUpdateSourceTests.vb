' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class ExternalDiagnosticUpdateSourceTests
        <Fact>
        Public Sub TestExternalDiagnostics_SupportGetDiagnostics()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim waiter = New Waiter()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Assert.True(source.SupportGetDiagnostics)
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_GetDiagnostics()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim waiter = New Waiter()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(workspace, project.Id)

                source.AddNewErrors(project.DocumentIds.First(), diagnostic)
                waiter.CreateWaitTask().PumpingWait()

                Dim data1 = source.GetDiagnostics(workspace, Nothing, Nothing, Nothing, CancellationToken.None)
                Assert.Equal(1, data1.Length)
                Assert.Equal(data1(0), diagnostic)

                source.ClearErrors(project.Id)
                waiter.CreateWaitTask().PumpingWait()

                Dim data2 = source.GetDiagnostics(workspace, Nothing, Nothing, Nothing, CancellationToken.None)
                Assert.Equal(0, data2.Length)
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_GetDiagnostics2()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim waiter = New Waiter()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(workspace, project.Id)

                Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(workspace, project.Id))))

                source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                waiter.CreateWaitTask().PumpingWait()

                Dim data1 = source.GetDiagnostics(workspace, Nothing, Nothing, Nothing, CancellationToken.None)
                Assert.Equal(2, data1.Length)

                source.ClearErrors(project.Id)
                waiter.CreateWaitTask().PumpingWait()

                Dim data2 = source.GetDiagnostics(workspace, Nothing, Nothing, Nothing, CancellationToken.None)
                Assert.Equal(0, data2.Length)
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_DuplicatedError()
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(String.Empty)
                Dim waiter = New Waiter()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(workspace, project.Id)

                Dim service = New TestDiagnosticAnalyzerService(ImmutableArray.Create(Of DiagnosticData)(diagnostic))
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(workspace, project.Id))))

                source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                source.OnSolutionBuild(Me, Shell.UIContextChangedEventArgs.From(False))
                waiter.CreateWaitTask().PumpingWait()

                Dim data1 = source.GetDiagnostics(workspace, Nothing, Nothing, Nothing, CancellationToken.None)
                Assert.Equal(1, data1.Length)

                source.ClearErrors(project.Id)
                waiter.CreateWaitTask().PumpingWait()

                Dim data2 = source.GetDiagnostics(workspace, Nothing, Nothing, Nothing, CancellationToken.None)
                Assert.Equal(0, data2.Length)
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalBuildErrorCustomTags()
            Assert.Equal(2, ProjectExternalErrorReporter.CustomTags.Count)
            Assert.Equal(WellKnownDiagnosticTags.Build, ProjectExternalErrorReporter.CustomTags(0))
        End Sub

        Private Function GetDiagnosticData(workspace As Workspace, projectId As ProjectId) As DiagnosticData
            Return New DiagnosticData(
                "Id", "Test", "Test Message", "Test Message Format", DiagnosticSeverity.Error, True, 0, workspace, projectId)
        End Function

        Private Class Waiter
            Inherits AsynchronousOperationListener
        End Class

        Private Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService, IDiagnosticUpdateSource

            Private ReadOnly data As ImmutableArray(Of DiagnosticData)

            Public Sub New()
            End Sub

            Public Sub New(data As ImmutableArray(Of DiagnosticData))
                Me.data = data
            End Sub

            Public ReadOnly Property SupportGetDiagnostics As Boolean Implements IDiagnosticUpdateSource.SupportGetDiagnostics
                Get
                    Return True
                End Get
            End Property

            Public Event DiagnosticsUpdated As EventHandler(Of DiagnosticsUpdatedArgs) Implements IDiagnosticUpdateSource.DiagnosticsUpdated

            Public Function GetDiagnostics(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, cancellationToken As CancellationToken) As ImmutableArray(Of DiagnosticData) Implements IDiagnosticUpdateSource.GetDiagnostics
                Return data
            End Function

            Public Sub Reanalyze(workspace As Workspace, Optional projectIds As IEnumerable(Of ProjectId) = Nothing, Optional documentIds As IEnumerable(Of DocumentId) = Nothing) Implements IDiagnosticAnalyzerService.Reanalyze
            End Sub

            Public Function GetDiagnosticDescriptors(projectOpt As Project) As ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)) Implements IDiagnosticAnalyzerService.GetDiagnosticDescriptors
                Return ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)).Empty
            End Function

            Public Function GetDiagnosticsForSpanAsync(document As Document, range As TextSpan, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync
                Return Task.FromResult(SpecializedCollections.EmptyEnumerable(Of DiagnosticData))
            End Function

            Public Function TryAppendDiagnosticsForSpanAsync(document As Document, range As TextSpan, diagnostics As List(Of DiagnosticData), cancellationToken As CancellationToken) As Task(Of Boolean) Implements IDiagnosticAnalyzerService.TryAppendDiagnosticsForSpanAsync
                Return Task.FromResult(False)
            End Function

            Public Function GetSpecificCachedDiagnosticsAsync(workspace As Workspace, id As Object, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetCachedDiagnosticsAsync(workspace As Workspace, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetSpecificDiagnosticsAsync(solution As Solution, id As Object, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetProjectDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticDescriptors(analyzer As DiagnosticAnalyzer) As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzerService.GetDiagnosticDescriptors
                Return ImmutableArray(Of DiagnosticDescriptor).Empty
            End Function
        End Class
    End Class
End Namespace
