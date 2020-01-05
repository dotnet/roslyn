' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ExtractInterface
#If False Then
    <[UseExportProvider]>
    Public Class EditAndContinueWorkspaceServiceTests
        <Fact>
        Public Sub ReadOnlyDocumentTest()
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueService = New EditAndContinueService(diagnosticService, New TestActiveStatementProvider())
            Dim workspace = EditAndContinueTestHelper.CreateTestWorkspace()
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
            Dim projectStates = ImmutableArray.Create(New KeyValuePair(Of ProjectId, ProjectReadOnlyReason)(project.Id, ProjectReadOnlyReason.None))

            encService.StartEditSession(currentSolution, projectStates.ToImmutableDictionary(), stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(False, isReadOnly)

            ' end edit session
            encService.EndEditSession(ImmutableDictionary(Of ActiveMethodId, ImmutableArray(Of NonRemappableRegion)).Empty)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.Running, sessionReason)
            Assert.Equal(True, isReadOnly)

            ' break mode and stop at exception
            encService.StartEditSession(currentSolution, projectStates.ToImmutableDictionary(), stoppedAtException:=True)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.None, projectReason)
            Assert.Equal(SessionReadOnlyReason.StoppedAtException, sessionReason)
            Assert.Equal(True, isReadOnly)
        End Sub

        <Fact>
        Public Sub NotLoadedTest()
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueService = New EditAndContinueService(diagnosticService, New TestActiveStatementProvider())
            Dim workspace = EditAndContinueTestHelper.CreateTestWorkspace()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean

            ' run mode 
            encService.StartDebuggingSession(workspace.CurrentSolution)

            ' edit mode
            Dim projectStates = ImmutableArray.Create(New KeyValuePair(Of ProjectId, ProjectReadOnlyReason)(project.Id, ProjectReadOnlyReason.NotLoaded))

            encService.StartEditSession(currentSolution, projectStates.ToImmutableDictionary(), stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.NotLoaded, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(True, isReadOnly)
        End Sub

        <Fact>
        Public Sub MetaDataNotAvailableTest()
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueService = New EditAndContinueService(diagnosticService, New TestActiveStatementProvider())
            Dim workspace = EditAndContinueTestHelper.CreateTestWorkspace()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean

            ' run mode 
            encService.StartDebuggingSession(workspace.CurrentSolution)

            ' edit mode with empty project
            Dim projectStates = ImmutableDictionary.Create(Of ProjectId, ProjectReadOnlyReason)

            encService.StartEditSession(currentSolution, projectStates, stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason)
            Assert.Equal(ProjectReadOnlyReason.MetadataNotAvailable, projectReason)
            Assert.Equal(SessionReadOnlyReason.None, sessionReason)
            Assert.Equal(True, isReadOnly)
        End Sub
    End Class
#End If
End Namespace
