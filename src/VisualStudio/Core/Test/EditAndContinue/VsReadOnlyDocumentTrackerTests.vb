' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Utilities
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue

    Public Class VsReadOnlyDocumentTrackerTests
        <WpfFact>
        Public Async Function StandardTextDocumentTest() As Threading.Tasks.Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim mockVsBuffer = New VsTextBufferMock()
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags)

            Dim mockEditorAdaptersFactoryService = New VsEditorAdaptersFactoryServiceMock(mockVsBuffer)
            Dim readOnlyDocumentTracker As VsReadOnlyDocumentTracker

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean
            Dim allowsReadOnly As Boolean = True 'It is a StandardTextDocument

            ' start debugging
            encService.StartDebuggingSession(workspace.CurrentSolution)
            readOnlyDocumentTracker = New VsReadOnlyDocumentTracker(encService, mockEditorAdaptersFactoryService, Nothing)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(1, mockVsBuffer._oldFlags) ' Read-Only

            ' edit mode
            Dim activeStatement = New Dictionary(Of DocumentId, ImmutableArray(Of ActiveStatementSpan))()
            Dim projectStates = ImmutableArray.Create(Of KeyValuePair(Of ProjectId, ProjectReadOnlyReason))(New KeyValuePair(Of ProjectId, ProjectReadOnlyReason)(project.Id, ProjectReadOnlyReason.None))

            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable

            ' end edit session
            encService.EndEditSession()
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(1, mockVsBuffer._oldFlags) ' Read-Only

            ' break mode and stop at exception
            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=True)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(1, mockVsBuffer._oldFlags) ' Read-Only
        End Function

        <WorkItem(1089964, "DevDiv")>
        <WpfFact>
        Public Async Function ContainedDocumentTest() As Threading.Tasks.Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim mockVsBuffer = New VsTextBufferMock()
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags)

            Dim mockEditorAdaptersFactoryService = New VsEditorAdaptersFactoryServiceMock(mockVsBuffer)
            Dim readOnlyDocumentTracker As VsReadOnlyDocumentTracker

            Dim sessionReason As SessionReadOnlyReason
            Dim projectReason As ProjectReadOnlyReason
            Dim isReadOnly As Boolean
            Dim allowsReadOnly As Boolean = False 'It is a ContainedDocument

            ' start debugging
            encService.StartDebuggingSession(workspace.CurrentSolution)
            readOnlyDocumentTracker = New VsReadOnlyDocumentTracker(encService, mockEditorAdaptersFactoryService, Nothing)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable

            ' edit mode
            Dim activeStatement = New Dictionary(Of DocumentId, ImmutableArray(Of ActiveStatementSpan))()
            Dim projectStates = ImmutableArray.Create(Of KeyValuePair(Of ProjectId, ProjectReadOnlyReason))(New KeyValuePair(Of ProjectId, ProjectReadOnlyReason)(project.Id, ProjectReadOnlyReason.None))

            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=False)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable

            ' end edit session
            encService.EndEditSession()
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable

            ' break mode and stop at exception
            encService.StartEditSession(currentSolution, activeStatement, projectStates.ToImmutableDictionary(), stoppedAtException:=True)
            isReadOnly = encService.IsProjectReadOnly(project.Id, sessionReason, projectReason) AndAlso allowsReadOnly
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), isReadOnly)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable
        End Function

        <WorkItem(1147868, "DevDiv")>
        <WpfFact>
        Public Async Function InvalidDocumentTest1() As Threading.Tasks.Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim mockVsBuffer = New VsTextBufferMock()
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags)

            Dim mockEditorAdaptersFactoryService = New VsEditorAdaptersFactoryServiceMock(mockVsBuffer)
            Dim readOnlyDocumentTracker As VsReadOnlyDocumentTracker

            ' start debugging & readOnlyDocumentTracker
            encService.StartDebuggingSession(workspace.CurrentSolution)
            readOnlyDocumentTracker = New VsReadOnlyDocumentTracker(encService, mockEditorAdaptersFactoryService, Nothing)

            ' valid document
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), False)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable

            ' invalid documentId
            readOnlyDocumentTracker.SetReadOnly(Nothing, False) ' Check no NRE
        End Function

        <WorkItem(1147868, "DevDiv")>
        <WpfFact>
        Public Async Function InvalidDocumentTest2() As Threading.Tasks.Task
            Dim diagnosticService As IDiagnosticAnalyzerService = New EditAndContinueTestHelper.TestDiagnosticAnalyzerService()
            Dim encService As IEditAndContinueWorkspaceService = New EditAndContinueWorkspaceService(diagnosticService)
            Dim workspace = Await EditAndContinueTestHelper.CreateTestWorkspaceAsync()
            Dim currentSolution = workspace.CurrentSolution
            Dim project = currentSolution.Projects(0)

            Dim mockVsBuffer = New VsTextBufferMock()
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags)

            Dim mockEditorAdaptersFactoryService = New VsEditorAdaptersFactoryServiceMock(mockVsBuffer)
            Dim readOnlyDocumentTracker As VsReadOnlyDocumentTracker

            ' start debugging & readOnlyDocumentTracker
            encService.StartDebuggingSession(workspace.CurrentSolution)
            readOnlyDocumentTracker = New VsReadOnlyDocumentTracker(encService, mockEditorAdaptersFactoryService, Nothing)

            ' valid document
            readOnlyDocumentTracker.SetReadOnly(project.DocumentIds.First(), False)
            Assert.Equal(Of UInteger)(0, mockVsBuffer._oldFlags) ' Editable

            ' the given project does not contain this document
            Dim newDocumentId = New DocumentId(New ProjectId(New Guid(), "TestProject"), New Guid(), "TestDoc")
            readOnlyDocumentTracker.SetReadOnly(newDocumentId, False) ' Check no NRE
        End Function

#Region "Helper Methods"

        Public Class VsTextBufferMock
            Implements IVsTextBuffer

            Public _oldFlags As UInteger = 0

            Public Function GetLanguageServiceID(ByRef pguidLangService As Guid) As Integer Implements IVsTextBuffer.GetLanguageServiceID
                Throw New NotImplementedException()
            End Function

            Public Function GetLastLineIndex(ByRef piLine As Integer, ByRef piIndex As Integer) As Integer Implements IVsTextBuffer.GetLastLineIndex
                Throw New NotImplementedException()
            End Function

            Public Function GetLengthOfLine(iLine As Integer, ByRef piLength As Integer) As Integer Implements IVsTextBuffer.GetLengthOfLine
                Throw New NotImplementedException()
            End Function

            Public Function GetLineCount(ByRef piLineCount As Integer) As Integer Implements IVsTextBuffer.GetLineCount
                Throw New NotImplementedException()
            End Function

            Public Function GetLineIndexOfPosition(iPosition As Integer, ByRef piLine As Integer, ByRef piColumn As Integer) As Integer Implements IVsTextBuffer.GetLineIndexOfPosition
                Throw New NotImplementedException()
            End Function

            Public Function GetPositionOfLine(iLine As Integer, ByRef piPosition As Integer) As Integer Implements IVsTextBuffer.GetPositionOfLine
                Throw New NotImplementedException()
            End Function

            Public Function GetPositionOfLineIndex(iLine As Integer, iIndex As Integer, ByRef piPosition As Integer) As Integer Implements IVsTextBuffer.GetPositionOfLineIndex
                Throw New NotImplementedException()
            End Function

            Public Function GetSize(ByRef piLength As Integer) As Integer Implements IVsTextBuffer.GetSize
                Throw New NotImplementedException()
            End Function

            Public Function GetStateFlags(ByRef pdwReadOnlyFlags As UInteger) As Integer Implements IVsTextBuffer.GetStateFlags
                pdwReadOnlyFlags = _oldFlags
                Return 0
            End Function

            Public Function GetUndoManager(ByRef ppUndoManager As IOleUndoManager) As Integer Implements IVsTextBuffer.GetUndoManager
                Throw New NotImplementedException()
            End Function

            Public Function InitializeContent(pszText As String, iLength As Integer) As Integer Implements IVsTextBuffer.InitializeContent
                Throw New NotImplementedException()
            End Function

            Public Function LockBuffer() As Integer Implements IVsTextBuffer.LockBuffer
                Throw New NotImplementedException()
            End Function

            Public Function LockBufferEx(dwFlags As UInteger) As Integer Implements IVsTextBuffer.LockBufferEx
                Throw New NotImplementedException()
            End Function

            Public Function Reload(fUndoable As Integer) As Integer Implements IVsTextBuffer.Reload
                Throw New NotImplementedException()
            End Function

            Public Function Reserved1() As Integer Implements IVsTextBuffer.Reserved1
                Throw New NotImplementedException()
            End Function

            Public Function Reserved10() As Integer Implements IVsTextBuffer.Reserved10
                Throw New NotImplementedException()
            End Function

            Public Function Reserved2() As Integer Implements IVsTextBuffer.Reserved2
                Throw New NotImplementedException()
            End Function

            Public Function Reserved3() As Integer Implements IVsTextBuffer.Reserved3
                Throw New NotImplementedException()
            End Function

            Public Function Reserved4() As Integer Implements IVsTextBuffer.Reserved4
                Throw New NotImplementedException()
            End Function

            Public Function Reserved5() As Integer Implements IVsTextBuffer.Reserved5
                Throw New NotImplementedException()
            End Function

            Public Function Reserved6() As Integer Implements IVsTextBuffer.Reserved6
                Throw New NotImplementedException()
            End Function

            Public Function Reserved7() As Integer Implements IVsTextBuffer.Reserved7
                Throw New NotImplementedException()
            End Function

            Public Function Reserved8() As Integer Implements IVsTextBuffer.Reserved8
                Throw New NotImplementedException()
            End Function

            Public Function Reserved9() As Integer Implements IVsTextBuffer.Reserved9
                Throw New NotImplementedException()
            End Function

            Public Function SetLanguageServiceID(ByRef guidLangService As Guid) As Integer Implements IVsTextBuffer.SetLanguageServiceID
                Throw New NotImplementedException()
            End Function

            Public Function SetStateFlags(dwReadOnlyFlags As UInteger) As Integer Implements IVsTextBuffer.SetStateFlags
                _oldFlags = dwReadOnlyFlags
                Return 0
            End Function

            Public Function UnlockBuffer() As Integer Implements IVsTextBuffer.UnlockBuffer
                Throw New NotImplementedException()
            End Function

            Public Function UnlockBufferEx(dwFlags As UInteger) As Integer Implements IVsTextBuffer.UnlockBufferEx
                Throw New NotImplementedException()
            End Function
        End Class

        Public Class VsEditorAdaptersFactoryServiceMock
            Implements IVsEditorAdaptersFactoryService

            Private _buffer As IVsTextBuffer
            Public Sub New(buffer As IVsTextBuffer)
                _buffer = buffer
            End Sub

            Public Sub SetDataBuffer(bufferAdapter As IVsTextBuffer, dataBuffer As ITextBuffer) Implements IVsEditorAdaptersFactoryService.SetDataBuffer
                Throw New NotImplementedException()
            End Sub

            Public Function CreateVsCodeWindowAdapter(serviceProvider As IServiceProvider) As IVsCodeWindow Implements IVsEditorAdaptersFactoryService.CreateVsCodeWindowAdapter
                Throw New NotImplementedException()
            End Function

            Public Function CreateVsTextBufferAdapter(serviceProvider As IServiceProvider) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferAdapter
                Throw New NotImplementedException()
            End Function

            Public Function CreateVsTextBufferAdapter(serviceProvider As IServiceProvider, contentType As IContentType) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferAdapter
                Throw New NotImplementedException()
            End Function

            Public Function CreateVsTextBufferAdapterForSecondaryBuffer(serviceProvider As IServiceProvider, secondaryBuffer As ITextBuffer) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferAdapterForSecondaryBuffer
                Throw New NotImplementedException()
            End Function

            Public Function CreateVsTextBufferCoordinatorAdapter() As IVsTextBufferCoordinator Implements IVsEditorAdaptersFactoryService.CreateVsTextBufferCoordinatorAdapter
                Throw New NotImplementedException()
            End Function

            Public Function CreateVsTextViewAdapter(serviceProvider As IServiceProvider) As IVsTextView Implements IVsEditorAdaptersFactoryService.CreateVsTextViewAdapter
                Throw New NotImplementedException()
            End Function

            Public Function CreateVsTextViewAdapter(serviceProvider As IServiceProvider, roles As ITextViewRoleSet) As IVsTextView Implements IVsEditorAdaptersFactoryService.CreateVsTextViewAdapter
                Throw New NotImplementedException()
            End Function

            Public Function GetBufferAdapter(textBuffer As ITextBuffer) As IVsTextBuffer Implements IVsEditorAdaptersFactoryService.GetBufferAdapter
                Return _buffer
            End Function

            Public Function GetDataBuffer(bufferAdapter As IVsTextBuffer) As ITextBuffer Implements IVsEditorAdaptersFactoryService.GetDataBuffer
                Throw New NotImplementedException()
            End Function

            Public Function GetDocumentBuffer(bufferAdapter As IVsTextBuffer) As ITextBuffer Implements IVsEditorAdaptersFactoryService.GetDocumentBuffer
                Throw New NotImplementedException()
            End Function

            Public Function GetViewAdapter(textView As ITextView) As IVsTextView Implements IVsEditorAdaptersFactoryService.GetViewAdapter
                Throw New NotImplementedException()
            End Function

            Public Function GetWpfTextView(viewAdapter As IVsTextView) As IWpfTextView Implements IVsEditorAdaptersFactoryService.GetWpfTextView
                Throw New NotImplementedException()
            End Function

            Public Function GetWpfTextViewHost(viewAdapter As IVsTextView) As IWpfTextViewHost Implements IVsEditorAdaptersFactoryService.GetWpfTextViewHost
                Throw New NotImplementedException()
            End Function
        End Class

#End Region

    End Class
End Namespace
