' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend NotInheritable Class VisualBasicProject
        Inherits AbstractLegacyProject
        Implements IVbCompilerProject

        Private ReadOnly _compilerHost As IVbCompilerHost

        Private _runtimeLibraries As ImmutableArray(Of String) = ImmutableArray(Of String).Empty

        ''' <summary>
        ''' To support the old contract of VB runtimes, we must ourselves add additional references beyond what the
        ''' project system tells us. If the project system _also_ tells us about those, we put the in here so we can
        ''' record that and make removal later work properly.
        ''' </summary>
        Private ReadOnly _explicitlyAddedRuntimeLibraries As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Friend Sub New(projectSystemName As String,
                       compilerHost As IVbCompilerHost,
                       hierarchy As IVsHierarchy,
                       isIntellisenseProject As Boolean,
                       serviceProvider As IServiceProvider,
                       threadingContext As IThreadingContext)
            MyBase.New(projectSystemName, hierarchy, LanguageNames.VisualBasic, isIntellisenseProject, serviceProvider, threadingContext, "VB")

            _compilerHost = compilerHost

            Dim componentModel = DirectCast(serviceProvider.GetService(GetType(SComponentModel)), IComponentModel)

            ProjectCodeModel = componentModel.GetService(Of IProjectCodeModelFactory).CreateProjectCodeModel(ProjectSystemProject.Id, New VisualBasicCodeModelInstanceFactory(Me))
            VisualStudioProjectOptionsProcessor = New OptionsProcessor(ProjectSystemProject, Workspace.Services.SolutionServices)
        End Sub

        Private Shadows Property VisualStudioProjectOptionsProcessor As OptionsProcessor
            Get
                Return DirectCast(MyBase.ProjectSystemProjectOptionsProcessor, OptionsProcessor)
            End Get
            Set(value As OptionsProcessor)
                MyBase.ProjectSystemProjectOptionsProcessor = value
            End Set
        End Property

        Public Sub AddApplicationObjectVariable(wszClassName As String, wszMemberName As String) Implements IVbCompilerProject.AddApplicationObjectVariable
            Throw New NotImplementedException()
        End Sub

        Public Sub AddBuffer(wszBuffer As String, dwLen As Integer, wszMkr As String, itemid As UInteger, fAdvise As Boolean, fShowErrorsInTaskList As Boolean) Implements IVbCompilerProject.AddBuffer
            Throw New NotImplementedException()
        End Sub

        Public Function AddEmbeddedMetaDataReference(wszFileName As String) As Integer Implements IVbCompilerProject.AddEmbeddedMetaDataReference
            ProjectSystemProject.AddMetadataReference(wszFileName, New MetadataReferenceProperties(embedInteropTypes:=True))
            Return VSConstants.S_OK
        End Function

        Public Overloads Function AddMetaDataReference(wszFileName As String, bAssembly As Boolean) As Integer Implements IVbCompilerProject.AddMetaDataReference
            ' If this is a reference already added due to it being a standard reference, just record the add
            If _runtimeLibraries.Contains(wszFileName, StringComparer.OrdinalIgnoreCase) Then
                _explicitlyAddedRuntimeLibraries.Add(wszFileName)
                Return VSConstants.S_OK
            Else
                ProjectSystemProject.AddMetadataReference(wszFileName, MetadataReferenceProperties.Assembly)
                Return VSConstants.S_OK
            End If
        End Function

        Public Sub AddEmbeddedProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.AddEmbeddedProjectReference
            Dim referencedProject = TryCast(pReferencedCompilerProject, VisualBasicProject)

            If referencedProject Is Nothing Then
                ' Hmm, we got a project which isn't from ourselves. That's somewhat odd, and we really can't do anything
                ' with it.
                Throw New ArgumentException("Unknown type of IVbCompilerProject.", NameOf(pReferencedCompilerProject))
            End If

            ProjectSystemProject.AddProjectReference(New ProjectReference(referencedProject.ProjectSystemProject.Id, embedInteropTypes:=True))
        End Sub

        Public Shadows Sub AddFile(wszFileName As String, itemid As UInteger, fAddDuringOpen As Boolean) Implements IVbCompilerProject.AddFile
            MyBase.AddFile(wszFileName, SourceCodeKind.Regular)
        End Sub

        Public Sub AddImport(wszImport As String) Implements IVbCompilerProject.AddImport
            VisualStudioProjectOptionsProcessor.AddImport(wszImport)
        End Sub

        Public Shadows Sub AddProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.AddProjectReference
            Dim referencedProject = TryCast(pReferencedCompilerProject, VisualBasicProject)

            If referencedProject Is Nothing Then
                ' Hmm, we got a project which isn't from ourselves. That's somewhat odd, and we really can't do anything
                ' with it.
                Throw New ArgumentException("Unknown type of IVbCompilerProject.", NameOf(pReferencedCompilerProject))
            End If

            ProjectSystemProject.AddProjectReference(New ProjectReference(referencedProject.ProjectSystemProject.Id))
        End Sub

        Public Sub AddResourceReference(wszFileName As String, wszName As String, fPublic As Boolean, fEmbed As Boolean) Implements IVbCompilerProject.AddResourceReference
            ' TODO: implement
        End Sub

#Region "Build Status Callbacks"

        ' AdviseBuildStatusCallback and UnadviseBuildStatusCallback do not accurately implement the
        ' contract that they imply (i.e. multiple listeners). This matches how they are implemented
        ' in the old VB code base: only one listener is allowed. This is a bit evil, but necessary
        ' since this needs to play nice with the old native project system.

        Private _buildStatusCallback As IVbBuildStatusCallback

        Public Function AdviseBuildStatusCallback(pIVbBuildStatusCallback As IVbBuildStatusCallback) As UInteger Implements IVbCompilerProject.AdviseBuildStatusCallback
            Try
                Debug.Assert(_buildStatusCallback Is Nothing, "IVbBuildStatusCallback already set")

                _buildStatusCallback = pIVbBuildStatusCallback

                If pIVbBuildStatusCallback IsNot Nothing Then
                    ' While Roslyn doesn't have the concept of a "bound" project, this call signals to the
                    ' project System that it can start the debugger hosting process.
                    ' Work Item#777487 tracks the removal of this concept in Dev14
                    pIVbBuildStatusCallback.ProjectBound()
                End If

                Return VSConstants.S_OK
            Catch e As Exception When FatalError.ReportAndPropagate(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Function

        Public Sub UnadviseBuildStatusCallback(dwCookie As UInteger) Implements IVbCompilerProject.UnadviseBuildStatusCallback
            Debug.Assert(dwCookie = 0, "Bad cookie")

            _buildStatusCallback = Nothing
        End Sub

#End Region
        Public Function CreateCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef ppCodeModel As EnvDTE.CodeModel) As Integer Implements IVbCompilerProject.CreateCodeModel
            ppCodeModel = ProjectCodeModel.GetOrCreateRootCodeModel(pProject)

            Return VSConstants.S_OK
        End Function

        Public Function CreateFileCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef ppFileCodeModel As EnvDTE.FileCodeModel) As Integer Implements IVbCompilerProject.CreateFileCodeModel
            ppFileCodeModel = Nothing

            If pProjectItem IsNot Nothing Then
                Dim fileName = pProjectItem.FileNames(1)

                If Not String.IsNullOrWhiteSpace(fileName) Then
                    ppFileCodeModel = ProjectCodeModel.GetOrCreateFileCodeModel(fileName, pProjectItem)
                    Return VSConstants.S_OK
                End If
            End If

            Return VSConstants.E_INVALIDARG
        End Function

        Public Sub DeleteAllImports() Implements IVbCompilerProject.DeleteAllImports
            VisualStudioProjectOptionsProcessor.DeleteAllImports()
        End Sub

        Public Sub DeleteAllResourceReferences() Implements IVbCompilerProject.DeleteAllResourceReferences
            ' TODO: implement
        End Sub

        Public Sub DeleteImport(wszImport As String) Implements IVbCompilerProject.DeleteImport
            VisualStudioProjectOptionsProcessor.DeleteImport(wszImport)
        End Sub

        Public Function ENCRebuild(in_pProgram As Object, ByRef out_ppUpdate As Object) As Integer Implements IVbCompilerProject.ENCRebuild
            Throw New NotSupportedException()
        End Function

        Public Function GetDefaultReferences(cElements As Integer, ByRef rgbstrReferences() As String, ByVal cActualReferences As IntPtr) As Integer Implements IVbCompilerProject.GetDefaultReferences
            Throw New NotImplementedException()
        End Function

        Public Sub GetEntryPointsList(cItems As Integer, strList() As String, ByVal pcActualItems As IntPtr) Implements IVbCompilerProject.GetEntryPointsList
            Dim project = Workspace.CurrentSolution.GetProject(ProjectSystemProject.Id)
            Dim compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)

            GetEntryPointsWorker(compilation, cItems, strList, pcActualItems, findFormsOnly:=False)
        End Sub

        Public Shared Sub GetEntryPointsWorker(compilation As Compilation,
                                               cItems As Integer,
                                               strList() As String,
                                               ByVal pcActualItems As IntPtr,
                                               findFormsOnly As Boolean)

            Dim entryPoints = EntryPointFinder.FindEntryPoints(compilation.SourceModule.GlobalNamespace, findFormsOnly:=findFormsOnly)

            ' If called with cItems = 0 and pcActualItems != NULL, GetEntryPointsList returns in pcActualItems the number of items available.
            If cItems = 0 AndAlso pcActualItems <> Nothing Then
                Marshal.WriteInt32(pcActualItems, entryPoints.Count())
                Exit Sub
            End If

            ' When called with cItems != 0, GetEntryPointsList assumes that there is
            ' enough space in strList[] for that many items, and fills up the array with those items
            ' (up to maximum available). Returns in pcActualItems the actual number of items that
            ' were put in the array. Assumes that the caller
            ' takes care of array allocation and de-allocation. 
            If cItems <> 0 Then
                Dim count = Math.Min(entryPoints.Count(), cItems)

                Dim names = entryPoints.Select(Function(p) p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat _
                    .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))) _
                    .ToArray()

                For i = 0 To count - 1
                    strList(i) = names(i)
                Next

                Marshal.WriteInt32(pcActualItems, count)
            End If
        End Sub

        Public Sub GetMethodFromLine(itemid As UInteger, iLine As Integer, ByRef pBstrProcName As String, ByRef pBstrClassName As String) Implements IVbCompilerProject.GetMethodFromLine
            Throw New NotImplementedException()
        End Sub

        Public Sub GetPEImage(ByRef ppImage As IntPtr) Implements IVbCompilerProject.GetPEImage
            Throw New NotImplementedException()
        End Sub

        Public Sub RemoveAllApplicationObjectVariables() Implements IVbCompilerProject.RemoveAllApplicationObjectVariables
            Throw New NotImplementedException()
        End Sub

        Public Sub RemoveAllReferences() Implements IVbCompilerProject.RemoveAllReferences
            Throw New NotImplementedException()
        End Sub

        Public Shadows Sub RemoveFile(wszFileName As String, itemid As UInteger) Implements IVbCompilerProject.RemoveFile
            MyBase.RemoveFile(wszFileName)
        End Sub

        Public Sub RemoveFileByName(wszPath As String) Implements IVbCompilerProject.RemoveFileByName
            Throw New NotImplementedException()
        End Sub

        Public Shadows Sub RemoveMetaDataReference(wszFileName As String) Implements IVbCompilerProject.RemoveMetaDataReference
            wszFileName = FileUtilities.NormalizeAbsolutePath(wszFileName)

            ' If this is a reference which was explicitly added and is also a runtime library, leave it
            If _explicitlyAddedRuntimeLibraries.Remove(wszFileName) Then
                Return
            End If

            ProjectSystemProject.RemoveMetadataReference(wszFileName, ProjectSystemProject.GetPropertiesForMetadataReference(wszFileName).Single())
        End Sub

        Public Shadows Sub RemoveProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.RemoveProjectReference
            Dim referencedProject = TryCast(pReferencedCompilerProject, VisualBasicProject)

            If referencedProject Is Nothing Then
                ' Hmm, we got a project which isn't from ourselves. That's somewhat odd, and we really can't do anything
                ' with it.
                Throw New ArgumentException("Unknown type of IVbCompilerProject.", NameOf(pReferencedCompilerProject))
            End If

            Dim projectReference = ProjectSystemProject.GetProjectReferences().Single(Function(p) p.ProjectId = referencedProject.ProjectSystemProject.Id)
            ProjectSystemProject.RemoveProjectReference(projectReference)
        End Sub

        Public Sub RenameDefaultNamespace(bstrDefaultNamespace As String) Implements IVbCompilerProject.RenameDefaultNamespace
            ' TODO: implement
        End Sub

        Public Sub RenameFile(wszOldFileName As String, wszNewFileName As String, itemid As UInteger) Implements IVbCompilerProject.RenameFile
            ' We treat the rename as a removal of the old file and the addition of a new one.
            RemoveFile(wszOldFileName, itemid)
            AddFile(wszNewFileName, itemid, fAddDuringOpen:=False)
        End Sub

        Public Sub RenameProject(wszNewProjectName As String) Implements IVbCompilerProject.RenameProject
            ' TODO: implement
        End Sub

        Public Sub SetBackgroundCompilerPriorityLow() Implements IVbCompilerProject.SetBackgroundCompilerPriorityLow
            ' We don't have a background compiler in Roslyn to set the priority of.
            Throw New NotSupportedException()
        End Sub

        Public Sub SetBackgroundCompilerPriorityNormal() Implements IVbCompilerProject.SetBackgroundCompilerPriorityNormal
            ' We don't have a background compiler in Roslyn to set the priority of.
            Throw New NotSupportedException()
        End Sub

        Public Sub SetCompilerOptions(ByRef pCompilerOptions As VBCompilerOptions) Implements IVbCompilerProject.SetCompilerOptions
            Dim oldRuntimeLibraries = _runtimeLibraries
            VisualStudioProjectOptionsProcessor.SetNewRawOptions(pCompilerOptions)

            If Not String.IsNullOrEmpty(pCompilerOptions.wszExeName) Then
                ProjectSystemProject.AssemblyName = Path.GetFileNameWithoutExtension(pCompilerOptions.wszExeName)

                ' Some legacy projects (e.g. Venus IntelliSense project) set '\' as the wszOutputPath.
                ' /src/venus/project/vb/vbprj/vbintelliproj.cpp
                ' Ignore paths that are not absolute.
                If Not String.IsNullOrEmpty(pCompilerOptions.wszOutputPath) Then
                    If PathUtilities.IsAbsolute(pCompilerOptions.wszOutputPath) Then
                        ProjectSystemProject.CompilationOutputAssemblyFilePath = Path.Combine(pCompilerOptions.wszOutputPath, pCompilerOptions.wszExeName)
                    Else
                        ProjectSystemProject.CompilationOutputAssemblyFilePath = Nothing
                    End If
                End If
            End If

            RefreshBinOutputPath()

            _runtimeLibraries = VisualStudioProjectOptionsProcessor.GetRuntimeLibraries(_compilerHost)

            If Not _runtimeLibraries.SequenceEqual(oldRuntimeLibraries, StringComparer.Ordinal) Then
                Using batchScope = ProjectSystemProject.CreateBatchScope()
                    ' To keep things simple, we'll just remove everything and add everything back in
                    For Each oldRuntimeLibrary In oldRuntimeLibraries
                        ' If this one was added explicitly in addition to our computation, we don't have to remove it 
                        If Not _explicitlyAddedRuntimeLibraries.Remove(oldRuntimeLibrary) Then
                            ProjectSystemProject.RemoveMetadataReference(oldRuntimeLibrary, MetadataReferenceProperties.Assembly)
                        End If
                    Next

                    For Each newRuntimeLibrary In _runtimeLibraries
                        newRuntimeLibrary = FileUtilities.NormalizeAbsolutePath(newRuntimeLibrary)

                        ' If we already reference this, just skip it
                        If ProjectSystemProject.ContainsMetadataReference(newRuntimeLibrary, MetadataReferenceProperties.Assembly) Then
                            _explicitlyAddedRuntimeLibraries.Add(newRuntimeLibrary)
                        Else
                            ProjectSystemProject.AddMetadataReference(newRuntimeLibrary, MetadataReferenceProperties.Assembly)
                        End If
                    Next
                End Using
            End If
        End Sub

        Public Sub SetModuleAssemblyName(wszName As String) Implements IVbCompilerProject.SetModuleAssemblyName
            Throw New NotImplementedException()
        End Sub

        Public Sub SetStreamForPDB(pStreamPDB As IStream) Implements IVbCompilerProject.SetStreamForPDB
            Throw New NotImplementedException()
        End Sub

        Public Sub StartBuild(pVsOutputWindowPane As IVsOutputWindowPane, fRebuildAll As Boolean) Implements IVbCompilerProject.StartBuild
            ' We currently have nothing to do for this.
        End Sub

        Public Sub StopBuild() Implements IVbCompilerProject.StopBuild
            ' We currently have nothing to do for this.
        End Sub

        Public Sub StartDebugging() Implements IVbCompilerProject.StartDebugging
            ' We currently have nothing to do for this.
        End Sub

        Public Sub StopDebugging() Implements IVbCompilerProject.StopDebugging
            ' We currently have nothing to do for this.
        End Sub

        Public Sub StartEdit() Implements IVbCompilerProject.StartEdit
            ' Since Roslyn's creation this method has not been implemented, because we didn't have a good batching concept in the project system shim code.
            ' We now have that (with VisualStudioProject.CreateBatchScope), but unfortunately clients are not very well behaved. The native language service
            ' relies on the old behavior, which was calling VisualBasicProject.StartBackgroundCompiler/VisualBasicProject.StopBackgroundCompiler and this
            ' method here all increment the same global counter in the end: it was OK to call StopBackgroundCompiler to stop it but FinishEdit() to restart it.
            ' Rather than trying to make this all work again, we'll leave this unimplemented still until we have evidence that this will help, and time to do it.
        End Sub

        Public Sub FinishEdit() Implements IVbCompilerProject.FinishEdit
            ' See comment in StartEdit for why this isn't implemented.
        End Sub

        Public Sub SuspendPostedNotifications() Implements IVbCompilerProject.SuspendPostedNotifications
            Throw New NotSupportedException()
        End Sub

        Public Sub ResumePostedNotifications() Implements IVbCompilerProject.ResumePostedNotifications
            Throw New NotSupportedException()
        End Sub

        Public Sub WaitUntilBound() Implements IVbCompilerProject.WaitUntilBound
            ' We no longer have a concept in Roslyn equivalent to the native WaitUntilBound, since we don't have a
            ' background compiler in the same sense.
            Throw New NotSupportedException()
        End Sub

        Public Shadows Sub Disconnect() Implements IVbCompilerProject.Disconnect
            MyBase.Disconnect()
        End Sub
    End Class
End Namespace
