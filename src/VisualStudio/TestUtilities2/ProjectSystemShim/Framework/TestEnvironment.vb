' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.ComponentModel.Composition.Hosting
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Moq
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework

    ''' <summary>
    ''' Class that holds onto the project tracker, fake IServiceProvider, and other interfaces needed to
    ''' unit test the project system shims outside of Visual Studio.
    ''' </summary>
    Friend Class TestEnvironment
        Implements IDisposable

        Friend Shared ReadOnly s_exportCatalog As Lazy(Of ComposableCatalog) = New Lazy(Of ComposableCatalog)(
            Function()
                Dim catalog = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                catalog = catalog.WithParts(GetType(FileChangeWatcherProvider),
                                            GetType(MockVisualStudioWorkspace),
                                            GetType(MetadataReferences.FileWatchedPortableExecutableReferenceFactory),
                                            GetType(VisualStudioProjectFactory),
                                            GetType(MockServiceProvider),
                                            GetType(SolutionEventsBatchScopeCreator),
                                            GetType(ProjectCodeModelFactory),
                                            GetType(CPSProjectFactory),
                                            GetType(VisualStudioRuleSetManagerFactory),
                                            GetType(VsMetadataServiceFactory),
                                            GetType(VisualStudioMetadataReferenceManagerFactory),
                                            GetType(MockWorkspaceEventListenerProvider))

                Return catalog
            End Function)

        Private ReadOnly _workspace As VisualStudioWorkspaceImpl
        Private ReadOnly _projectFilePaths As New List(Of String)

        Public Sub New(Optional solutionIsFullyLoaded As Boolean = True, Optional exportProviderFactory As IExportProviderFactory = Nothing)

            If exportProviderFactory Is Nothing Then
                exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(s_exportCatalog.Value)
            End If

            ExportProvider = exportProviderFactory.CreateExportProvider()
            _workspace = ExportProvider.GetExportedValue(Of VisualStudioWorkspaceImpl)
            ThreadingContext = ExportProvider.GetExportedValue(Of IThreadingContext)()
            Interop.WrapperPolicy.s_ComWrapperFactory = MockComWrapperFactory.Instance

            Dim mockServiceProvider As MockServiceProvider = ExportProvider.GetExportedValue(Of MockServiceProvider)()
            mockServiceProvider.MockMonitorSelection = New MockShellMonitorSelection(solutionIsFullyLoaded)
            ServiceProvider = mockServiceProvider
        End Sub

        Public ReadOnly Property ProjectFactory As VisualStudioProjectFactory
            Get
                Return ExportProvider.GetExportedValue(Of VisualStudioProjectFactory)
            End Get
        End Property

        <PartNotDiscoverable>
        <Export(GetType(VisualStudioWorkspace))>
        <Export(GetType(VisualStudioWorkspaceImpl))>
        Private Class MockVisualStudioWorkspace
            Inherits VisualStudioWorkspaceImpl

            <ImportingConstructor>
            Public Sub New(exportProvider As Composition.ExportProvider)
                MyBase.New(exportProvider, exportProvider.GetExportedValue(Of MockServiceProvider))
            End Sub

            Public Overrides Sub DisplayReferencedSymbols(solution As Microsoft.CodeAnalysis.Solution, referencedSymbols As IEnumerable(Of ReferencedSymbol))
                Throw New NotImplementedException()
            End Sub

            Public Overrides Function TryGoToDefinition(symbol As ISymbol, project As Microsoft.CodeAnalysis.Project, cancellationToken As CancellationToken) As Boolean
                Throw New NotImplementedException()
            End Function

            Public Overrides Function TryFindAllReferences(symbol As ISymbol, project As Microsoft.CodeAnalysis.Project, cancellationToken As CancellationToken) As Boolean
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function OpenInvisibleEditor(documentId As DocumentId) As IInvisibleEditor
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function GetBrowseObject(symbolListItem As SymbolListItem) As Object
                Throw New NotImplementedException()
            End Function
        End Class

        Public ReadOnly Property ThreadingContext As IThreadingContext
        Public ReadOnly Property ServiceProvider As IServiceProvider
        Public ReadOnly Property ExportProvider As Composition.ExportProvider

        Public ReadOnly Property Workspace As VisualStudioWorkspaceImpl
            Get
                Return _workspace
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            _workspace.Dispose()

            For Each filePath In _projectFilePaths
                File.Delete(filePath)
                Directory.Delete(Path.GetDirectoryName(filePath))
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

        Public Function CreateHierarchy(projectName As String, projectBinPath As String, projectRefPath As String, projectCapabilities As String) As IVsHierarchy
            Return New MockHierarchy(projectName, CreateProjectFile(projectName), projectBinPath, projectRefPath, projectCapabilities)
        End Function

        Public Function GetUpdatedCompilationOptionOfSingleProject() As CompilationOptions
            Return Workspace.CurrentSolution.Projects.Single().CompilationOptions
        End Function

        <PartNotDiscoverable>
        <Export>
        <Export(GetType(SVsServiceProvider))>
        Private Class MockServiceProvider
            Implements System.IServiceProvider
            Implements SVsServiceProvider ' The shell service provider actually implements this too for people using that type directly
            Implements Shell.IAsyncServiceProvider

            Private ReadOnly _exportProvider As Composition.ExportProvider
            Private ReadOnly _fileChangeEx As MockVsFileChangeEx = New MockVsFileChangeEx

            Public MockMonitorSelection As IVsMonitorSelection

            <ImportingConstructor>
            Public Sub New(exportProvider As Composition.ExportProvider)
                _exportProvider = exportProvider
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
                        Return MockMonitorSelection

                    Case GetType(SVsXMLMemberIndexService)
                        Return New MockXmlMemberIndexService

                    Case GetType(SVsSmartOpenScope)
                        Return New MockVsSmartOpenScope

                    Case GetType(SVsFileChangeEx)
                        Return _fileChangeEx

                    Case Else
                        Throw New Exception($"{NameOf(MockServiceProvider)} does not implement {serviceType.FullName}.")
                End Select
            End Function

            Public Function GetServiceAsync(serviceType As Type) As Task(Of Object) Implements Shell.IAsyncServiceProvider.GetServiceAsync
                Return System.Threading.Tasks.Task.FromResult(GetService(serviceType))
            End Function

            Friend Function GetComponentModelMock() As IComponentModel
                Return New MockComponentModel(_exportProvider)
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

        Friend Sub RaiseFileChange(path As String)
            ' Ensure we've pushed everything to the file change watcher
            Dim fileChangeProvider = ExportProvider.GetExportedValue(Of FileChangeWatcherProvider)
            Dim mockFileChangeService = DirectCast(ServiceProvider.GetService(GetType(SVsFileChangeEx)), MockVsFileChangeEx)
            fileChangeProvider.TrySetFileChangeService_TestOnly(mockFileChangeService)
            fileChangeProvider.Watcher.WaitForQueue_TestOnly()
            mockFileChangeService.FireUpdate(path)
        End Sub

        Private Class MockVsSmartOpenScope
            Implements IVsSmartOpenScope

            Public Function OpenScope(wszScope As String, dwOpenFlags As UInteger, ByRef riid As Guid, ByRef ppIUnk As Object) As Integer Implements IVsSmartOpenScope.OpenScope
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
