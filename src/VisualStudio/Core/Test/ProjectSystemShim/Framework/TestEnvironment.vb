' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Text
Imports Moq

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework

    ''' <summary>
    ''' Class that holds onto the project tracker, fake IServiceProvider, and other interfaces needed to
    ''' unit test the project system shims outside of Visual Studio.
    ''' </summary>
    Friend Class TestEnvironment
        Implements IDisposable

        Private ReadOnly _monitorSelectionMock As MockShellMonitorSelection
        Private ReadOnly _projectTracker As VisualStudioProjectTracker
        Private ReadOnly _serviceProvider As MockServiceProvider
        Private ReadOnly _workspace As TestWorkspace

        Public Sub New(Optional solutionIsFullyLoaded As Boolean = True)
            ' As a policy, if anything goes wrong don't use exception filters, just throw exceptions for the
            ' test harness to catch normally. Otherwise debugging things can be annoying when your test process
            ' goes away
            AbstractProject.CrashOnException = False

            _monitorSelectionMock = New MockShellMonitorSelection(solutionIsFullyLoaded)
            _serviceProvider = New MockServiceProvider(_monitorSelectionMock)
            _projectTracker = New VisualStudioProjectTracker(_serviceProvider)
            _workspace = New TestWorkspace()
            _projectTracker.MetadataReferenceProvider = New VisualStudioMetadataReferenceManager(_serviceProvider, _workspace.Services.GetService(Of ITemporaryStorageService)())
            _projectTracker.RuleSetFileProvider = New VisualStudioRuleSetManager(
                DirectCast(_serviceProvider.GetService(GetType(SVsFileChangeEx)), IVsFileChangeEx),
                New TestForegroundNotificationService(),
                AggregateAsynchronousOperationListener.CreateEmptyListener())

            Dim workspaceHost = New WorkspaceHost(_workspace)
            _projectTracker.RegisterWorkspaceHost(workspaceHost)
            _projectTracker.StartSendingEventsToWorkspaceHost(workspaceHost)
        End Sub

        Public Sub NotifySolutionAsFullyLoaded()
            _monitorSelectionMock.SolutionIsFullyLoaded = True
            GetSolutionLoadEvents().OnAfterBackgroundSolutionLoadComplete()
        End Sub

        Public Function GetSolutionLoadEvents() As IVsSolutionLoadEvents
            Return DirectCast(_projectTracker, IVsSolutionLoadEvents)
        End Function

        Public ReadOnly Property ProjectTracker As VisualStudioProjectTracker
            Get
                Return _projectTracker
            End Get
        End Property

        Public ReadOnly Property ServiceProvider As MockServiceProvider
            Get
                Return _serviceProvider
            End Get
        End Property

        Public ReadOnly Property Workspace As Workspace
            Get
                Return _workspace
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            For Each project In _projectTracker.Projects.ToArray()
                project.Disconnect()
            Next

            _workspace.Dispose()
            _projectTracker.Dispose()
        End Sub

        Public Function CreateHierarchy(projectName As String, projectCapabilities As String) As IVsHierarchy
            Return New MockHierarchy(projectName, projectCapabilities)
        End Function

        Public Function GetUpdatedCompilationOptionOfSingleProject() As CompilationOptions
            Return Workspace.CurrentSolution.Projects.Single().CompilationOptions
        End Function

        Friend Class MockServiceProvider
            Implements System.IServiceProvider

            Private ReadOnly _mockMonitorSelection As IVsMonitorSelection

            Public Sub New(mockMonitorSelection As IVsMonitorSelection)
                _mockMonitorSelection = mockMonitorSelection
            End Sub

            Public Function GetService(serviceType As Type) As Object Implements System.IServiceProvider.GetService
                Select Case serviceType
                    Case GetType(SVsSolution)
                        ' Return a loose mock that just is a big no-op
                        Dim solutionMock As New Mock(Of IVsSolution)(MockBehavior.Loose)
                        Return solutionMock.Object

                    Case GetType(SComponentModel)
                        Return GetComponentModelMock()

                    Case GetType(SVsShellMonitorSelection)
                        Return _mockMonitorSelection

                    Case GetType(SVsXMLMemberIndexService)
                        Return New MockXmlMemberIndexService

                    Case GetType(SVsSmartOpenScope)
                        Return New MockVsSmartOpenScope

                    Case GetType(SVsFileChangeEx)
                        Return New MockVsFileChangeEx

                    Case Else
                        Return Nothing
                End Select
            End Function

            Friend Function GetComponentModelMock() As IComponentModel
                Dim componentModel As New Mock(Of IComponentModel)(MockBehavior.Loose)
                componentModel.SetupGet(Function(cm) cm.DefaultExportProvider).Returns(ExportProvider.AsExportProvider())
                Return componentModel.Object
            End Function
        End Class

        Private Class MockShellMonitorSelection
            Implements IVsMonitorSelection

            Public Property SolutionIsFullyLoaded As Boolean

            Public Sub New(solutionIsFullyLoaded As Boolean)
                Me.SolutionIsFullyLoaded = solutionIsFullyLoaded
            End Sub

            Public Function AdviseSelectionEvents(pSink As IVsSelectionEvents, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> ByRef pdwCookie As UInteger) As Integer Implements IVsMonitorSelection.AdviseSelectionEvents
                Throw New NotImplementedException()
            End Function

            Public Function GetCmdUIContextCookie(<ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")> ByRef rguidCmdUI As Guid, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> ByRef pdwCmdUICookie As UInteger) As Integer Implements IVsMonitorSelection.GetCmdUIContextCookie
                Return VSConstants.S_OK
            End Function

            Public Function GetCurrentElementValue(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSELELEMID")> elementid As UInteger, ByRef pvarValue As Object) As Integer Implements IVsMonitorSelection.GetCurrentElementValue
                Throw New NotImplementedException()
            End Function

            Public Function GetCurrentSelection(ByRef ppHier As IntPtr, <ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")> ByRef pitemid As UInteger, ByRef ppMIS As IVsMultiItemSelect, ByRef ppSC As IntPtr) As Integer Implements IVsMonitorSelection.GetCurrentSelection
                Throw New NotImplementedException()
            End Function

            Public Function IsCmdUIContextActive(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> dwCmdUICookie As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> ByRef pfActive As Integer) As Integer Implements IVsMonitorSelection.IsCmdUIContextActive
                ' Be lazy and don't worry checking which cookie this is, since for now the VisualStudioProjectTracker only checks for one
                pfActive = If(SolutionIsFullyLoaded, 1, 0)
                Return VSConstants.S_OK
            End Function

            Public Function SetCmdUIContext(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> dwCmdUICookie As UInteger, <ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")> fActive As Integer) As Integer Implements IVsMonitorSelection.SetCmdUIContext
                Throw New NotImplementedException()
            End Function

            Public Function UnadviseSelectionEvents(<ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")> dwCookie As UInteger) As Integer Implements IVsMonitorSelection.UnadviseSelectionEvents
                Throw New NotImplementedException()
            End Function
        End Class

        Private Class WorkspaceHost
            Implements IVisualStudioWorkspaceHost, IVisualStudioWorkspaceHost2

            Private _workspace As TestWorkspace

            Public Sub New(workspace As TestWorkspace)
                _workspace = workspace
            End Sub

            Public Sub ClearSolution() Implements IVisualStudioWorkspaceHost.ClearSolution
                _workspace.ClearSolution()
            End Sub

            Public Sub OnAdditionalDocumentAdded(additionalDocument As DocumentInfo) Implements IVisualStudioWorkspaceHost.OnAdditionalDocumentAdded
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAdditionalDocumentClosed(documentId As DocumentId, textBuffer As ITextBuffer, loader As TextLoader) Implements IVisualStudioWorkspaceHost.OnAdditionalDocumentClosed
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAdditionalDocumentOpened(documentId As DocumentId, textBuffer As ITextBuffer, isCurrentContext As Boolean) Implements IVisualStudioWorkspaceHost.OnAdditionalDocumentOpened
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAdditionalDocumentRemoved(additionalDocument As DocumentId) Implements IVisualStudioWorkspaceHost.OnAdditionalDocumentRemoved
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAdditionalDocumentTextUpdatedOnDisk(id As DocumentId) Implements IVisualStudioWorkspaceHost.OnAdditionalDocumentTextUpdatedOnDisk
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAnalyzerReferenceAdded(projectId As ProjectId, analyzerReference As AnalyzerReference) Implements IVisualStudioWorkspaceHost.OnAnalyzerReferenceAdded
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAnalyzerReferenceRemoved(projectId As ProjectId, analyzerReference As AnalyzerReference) Implements IVisualStudioWorkspaceHost.OnAnalyzerReferenceRemoved
                Throw New NotImplementedException()
            End Sub

            Public Sub OnAssemblyNameChanged(id As ProjectId, assemblyName As String) Implements IVisualStudioWorkspaceHost.OnAssemblyNameChanged
                _workspace.OnAssemblyNameChanged(id, assemblyName)
            End Sub

            Public Sub OnDocumentAdded(documentInfo As DocumentInfo) Implements IVisualStudioWorkspaceHost.OnDocumentAdded
                Throw New NotImplementedException()
            End Sub

            Public Sub OnDocumentClosed(documentId As DocumentId, textBuffer As ITextBuffer, loader As TextLoader, updateActiveContext As Boolean) Implements IVisualStudioWorkspaceHost.OnDocumentClosed
                Throw New NotImplementedException()
            End Sub

            Public Sub OnDocumentOpened(documentId As DocumentId, textBuffer As ITextBuffer, isCurrentContext As Boolean) Implements IVisualStudioWorkspaceHost.OnDocumentOpened
                Throw New NotImplementedException()
            End Sub

            Public Sub OnDocumentRemoved(documentId As DocumentId) Implements IVisualStudioWorkspaceHost.OnDocumentRemoved
                Throw New NotImplementedException()
            End Sub

            Public Sub OnDocumentTextUpdatedOnDisk(id As DocumentId) Implements IVisualStudioWorkspaceHost.OnDocumentTextUpdatedOnDisk
                Throw New NotImplementedException()
            End Sub

            Public Sub OnHasAllInformation(projectId As ProjectId, hasAllInformation As Boolean) Implements IVisualStudioWorkspaceHost2.OnHasAllInformation
                Throw New NotImplementedException()
            End Sub

            Public Sub UpdateGeneratedDocumentsIfNecessary(projectInfo As ProjectId) Implements IVisualStudioWorkspaceHost2.UpdateGeneratedDocumentsIfNecessary
                Throw New NotImplementedException()
            End Sub

            Public Sub OnMetadataReferenceAdded(projectId As ProjectId, metadataReference As PortableExecutableReference) Implements IVisualStudioWorkspaceHost.OnMetadataReferenceAdded
                _workspace.OnMetadataReferenceAdded(projectId, metadataReference)
            End Sub

            Public Sub OnMetadataReferenceRemoved(projectId As ProjectId, metadataReference As PortableExecutableReference) Implements IVisualStudioWorkspaceHost.OnMetadataReferenceRemoved
                _workspace.OnMetadataReferenceRemoved(projectId, metadataReference)
            End Sub

            Public Sub OnOptionsChanged(projectId As ProjectId, compilationOptions As CompilationOptions, parseOptions As ParseOptions) Implements IVisualStudioWorkspaceHost.OnOptionsChanged
                _workspace.OnCompilationOptionsChanged(projectId, compilationOptions)
                _workspace.OnParseOptionsChanged(projectId, parseOptions)
            End Sub

            Public Sub OnOutputFilePathChanged(id As ProjectId, outputFilePath As String) Implements IVisualStudioWorkspaceHost.OnOutputFilePathChanged
                _workspace.OnOutputFilePathChanged(id, outputFilePath)
            End Sub

            Public Sub OnProjectAdded(projectInfo As ProjectInfo) Implements IVisualStudioWorkspaceHost.OnProjectAdded
                _workspace.OnProjectAdded(projectInfo)
            End Sub

            Public Sub OnProjectNameChanged(projectId As ProjectId, name As String, filePath As String) Implements IVisualStudioWorkspaceHost.OnProjectNameChanged
                _workspace.OnProjectNameChanged(projectId, name, filePath)
            End Sub

            Public Sub OnProjectReferenceAdded(projectId As ProjectId, projectReference As ProjectReference) Implements IVisualStudioWorkspaceHost.OnProjectReferenceAdded
                _workspace.OnProjectReferenceAdded(projectId, projectReference)
            End Sub

            Public Sub OnProjectReferenceRemoved(projectId As ProjectId, projectReference As ProjectReference) Implements IVisualStudioWorkspaceHost.OnProjectReferenceRemoved
                _workspace.OnProjectReferenceRemoved(projectId, projectReference)
            End Sub

            Public Sub OnProjectRemoved(projectId As ProjectId) Implements IVisualStudioWorkspaceHost.OnProjectRemoved
                _workspace.OnProjectRemoved(projectId)
            End Sub

            Public Sub OnSolutionAdded(solutionInfo As SolutionInfo) Implements IVisualStudioWorkspaceHost.OnSolutionAdded
                _workspace.OnSolutionAdded(solutionInfo)
            End Sub

            Public Sub OnSolutionRemoved() Implements IVisualStudioWorkspaceHost.OnSolutionRemoved
                _workspace.OnSolutionRemoved()
            End Sub
        End Class

        Private Class MockXmlMemberIndexService
            Implements IVsXMLMemberIndexService

            Public Function CreateXMLMemberIndex(pszBinaryName As String, ByRef ppIndex As IVsXMLMemberIndex) As Integer Implements IVsXMLMemberIndexService.CreateXMLMemberIndex
                Throw New NotImplementedException()
            End Function

            Public Function GetMemberDataFromXML(pszXML As String, ByRef ppObj As IVsXMLMemberData) As Integer Implements IVsXMLMemberIndexService.GetMemberDataFromXML
                Throw New NotImplementedException()
            End Function
        End Class

        Private Class MockVsSmartOpenScope
            Implements IVsSmartOpenScope

            Public Function OpenScope(wszScope As String, dwOpenFlags As UInteger, ByRef riid As Guid, ByRef ppIUnk As Object) As Integer Implements IVsSmartOpenScope.OpenScope
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
