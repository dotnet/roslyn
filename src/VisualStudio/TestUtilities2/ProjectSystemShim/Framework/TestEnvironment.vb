' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation
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
        Private ReadOnly _projectFilePaths As New List(Of String)

        Public Sub New(Optional solutionIsFullyLoaded As Boolean = True)
            ' As a policy, if anything goes wrong don't use exception filters, just throw exceptions for the
            ' test harness to catch normally. Otherwise debugging things can be annoying when your test process
            ' goes away
            AbstractProject.CrashOnException = False

            _monitorSelectionMock = New MockShellMonitorSelection(solutionIsFullyLoaded)
            _serviceProvider = New MockServiceProvider(_monitorSelectionMock)
            _workspace = New TestWorkspace()
            _projectTracker = New VisualStudioProjectTracker(_serviceProvider, _workspace)

            Dim metadataReferenceProvider = New VisualStudioMetadataReferenceManager(_serviceProvider, _workspace.Services.GetService(Of ITemporaryStorageService)())
            Dim ruleSetFileProvider = New VisualStudioRuleSetManager(
                DirectCast(_serviceProvider.GetService(GetType(SVsFileChangeEx)), IVsFileChangeEx),
                New TestForegroundNotificationService(),
                AsynchronousOperationListenerProvider.NullListener)

            Dim documentTrackingService = New VisualStudioDocumentTrackingService(_serviceProvider)
            Dim documentProvider = New DocumentProvider(_projectTracker, _serviceProvider, documentTrackingService)

            _projectTracker.InitializeProviders(documentProvider, metadataReferenceProvider, ruleSetFileProvider)
        End Sub

        Public Sub NotifySolutionAsFullyLoaded()
            _monitorSelectionMock.SolutionIsFullyLoaded = True
            _projectTracker.OnAfterBackgroundSolutionLoadComplete()
        End Sub

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

        Public ReadOnly Property Workspace As Microsoft.CodeAnalysis.Workspace
            Get
                Return _workspace
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            For Each project In _projectTracker.ImmutableProjects.ToArray()
                project.Disconnect()
            Next

            _projectTracker.OnAfterCloseSolution()
            _workspace.Dispose()

            For Each filePath In _projectFilePaths
                File.Delete(filePath)
            Next
        End Sub

        Private Function CreateProjectFile(projectName As String) As String
            Dim dir = Path.Combine(Path.GetTempPath, Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory(dir)
            Dim result = Path.Combine(dir, projectName + ".vbproj")
            File.WriteAllText(result, "<Project></Project>")
            _projectFilePaths.Add(result)
            Return result
        End Function

        Public Function CreateHierarchy(projectName As String, projectBinPath As String, projectCapabilities As String) As IVsHierarchy
            Return New MockHierarchy(projectName, CreateProjectFile(projectName), projectBinPath, projectCapabilities)
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
                Return VSConstants.S_OK
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
