' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
    Public Class EditAndContinueWorkspaceServiceTests
        <Fact>
        Public Async Function ReadOnlyDocumentTest() As Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean

            ' not yet start
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)

            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(False, isReadOnly)

            ' run mode 
            encService.StartDebuggingSession(workspace.CurrentSolution)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)

            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.Running, sessionReason)
            Assert.Equal(True, isReadOnly)

            ' edit mode
            Dim activeStatement = New Dictionary(Of DocumentId, ImmutableArray(Of ActiveStatementSpan))()
            Dim projectStates = ImmutableArray.Create(Of KeyValuePair(Of ProjectId, ProjectReadOnlyReason))(New KeyValuePair(Of ProjectId, ProjectReadOnlyReason)(project.Id, ProjectReadOnlyReason.None))

            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(False, isReadOnly)

            ' end edit session
            encService.EndEditSession()
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.Running, sessionReason)
            Assert.Equal(True, isReadOnly)

            ' break mode and stop at exception
            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=True)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.StoppedAtException, sessionReason)
            Assert.Equal(True, isReadOnly)
        End Function

        <Fact>
        Public Async Function NotLoadedTest() As Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean

            ' run mode 
            encService.StartDebuggingSession(workspace.CurrentSolution)

            ' edit mode
            Dim activeStatement = New Dictionary(Of DocumentId, ImmutableArray(Of ActiveStatementSpan))()
            Dim projectStates = ImmutableArray.Create(Of KeyValuePair(Of ProjectId, ProjectReadOnlyReason))(New KeyValuePair(Of ProjectId, ProjectReadOnlyReason)(project.Id, ProjectReadOnlyReason.NotLoaded))

            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.NotLoaded, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(True, isReadOnly)
        End Function

        <Fact>
        Public Async Function MetaDataNotAvailableTest() As Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean

            ' run mode 
            encService.StartDebuggingSession(workspace.CurrentSolution)

            ' edit mode with empty project
            Dim activeStatement = New Dictionary(Of DocumentId, ImmutableArray(Of ActiveStatementSpan))()
            Dim projectStates = ImmutableDictionary.Create(Of ProjectId, ProjectReadOnlyReason)

            encService.StartEditSession(currentSolution, activeStatement, projectStates, stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.MetadataNotAvailable, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(True, isReadOnly)
        End Function
    End Class

End Namespace
