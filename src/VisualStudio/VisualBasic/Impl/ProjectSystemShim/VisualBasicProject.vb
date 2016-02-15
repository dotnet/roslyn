' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend MustInherit Class VisualBasicProject
        Inherits AbstractRoslynProject
        Implements IVbCompilerProject
        Implements IVisualStudioHostProject

        Private ReadOnly _compilerHost As IVbCompilerHost
        Private ReadOnly _imports As New List(Of GlobalImport)
        Private _lastOptions As ConvertedVisualBasicProjectOptions = ConvertedVisualBasicProjectOptions.EmptyOptions
        Private _rawOptions As VBCompilerOptions
        Private ReadOnly _explicitlyAddedDefaultReferences As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' Maps a string to the related <see cref="GlobalImport"/>. Since many projects in a solution
        ''' will have similar (if not identical) sets of imports, there are performance benefits to
        ''' caching these rather than parsing them anew for each project. It is expected that the total
        ''' number of imports will be rather small, which is why we never evict anything from this cache.
        ''' </summary>
        Private Shared s_importsCache As Dictionary(Of String, GlobalImport) = New Dictionary(Of String, GlobalImport)

        Friend Sub New(projectTracker As VisualStudioProjectTracker,
                       ProjectSystemName As String,
                       compilerHost As IVbCompilerHost,
                       hierarchy As IVsHierarchy,
                       serviceProvider As IServiceProvider,
                       reportExternalErrorCreatorOpt As Func(Of ProjectId, IVsReportExternalErrors),
                       miscellaneousFilesWorkspaceOpt As MiscellaneousFilesWorkspace,
                       visualStudioWorkspaceOpt As VisualStudioWorkspaceImpl,
                       hostDiagnosticUpdateSourceOpt As HostDiagnosticUpdateSource)
            MyBase.New(projectTracker, reportExternalErrorCreatorOpt, ProjectSystemName, hierarchy, LanguageNames.VisualBasic, serviceProvider, miscellaneousFilesWorkspaceOpt, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt)

            _compilerHost = compilerHost

            projectTracker.AddProject(Me)
        End Sub

        Public Sub AddApplicationObjectVariable(wszClassName As String, wszMemberName As String) Implements IVbCompilerProject.AddApplicationObjectVariable
            Throw New NotImplementedException()
        End Sub

        Public Sub AddBuffer(wszBuffer As String, dwLen As Integer, wszMkr As String, itemid As UInteger, fAdvise As Boolean, fShowErrorsInTaskList As Boolean) Implements IVbCompilerProject.AddBuffer
            Throw New NotImplementedException()
        End Sub

        Public Function AddEmbeddedMetaDataReference(wszFileName As String) As Integer Implements IVbCompilerProject.AddEmbeddedMetaDataReference
            Try
                Return AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(wszFileName, New MetadataReferenceProperties(embedInteropTypes:=True), VSConstants.S_FALSE)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Function

        Public Overloads Function AddMetaDataReference(wszFileName As String, bAssembly As Boolean) As Integer Implements IVbCompilerProject.AddMetaDataReference
            Try
                ' If this is a reference already added due to it being a standard reference, just record the add
                If _lastOptions.RuntimeLibraries.Contains(wszFileName, StringComparer.OrdinalIgnoreCase) Then
                    _explicitlyAddedDefaultReferences.Add(wszFileName)
                    Return VSConstants.S_OK
                End If

                Return AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(wszFileName, New MetadataReferenceProperties(), VSConstants.S_FALSE)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Function

        Public Sub AddEmbeddedProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.AddEmbeddedProjectReference
            Try
                Dim project = TryCast(pReferencedCompilerProject, VisualBasicProject)

                If project Is Nothing Then
                    ' Hmm, we got a project which isn't from ourselves. That's somewhat odd, and we really can't do anything
                    ' with it.
                    Throw New ArgumentException("Unknown type of IVbCompilerProject.", NameOf(pReferencedCompilerProject))
                End If

                MyBase.AddProjectReference(New ProjectReference(project.Id, embedInteropTypes:=True))
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Shadows Sub AddFile(wszFileName As String, itemid As UInteger, fAddDuringOpen As Boolean) Implements IVbCompilerProject.AddFile
            Try
                ' We trust the project system to only tell us about files that we can use.
                Dim canUseTextBuffer As Func(Of ITextBuffer, Boolean) = Function(t) True

                MyBase.AddFile(wszFileName, SourceCodeKind.Regular, itemid, canUseTextBuffer)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Sub AddImport(wszImport As String) Implements IVbCompilerProject.AddImport
            Try
                ' Add the import to the list. The legacy language services didn't do any sort of
                ' checking to see if the import is already added. Instead, they'd just have two entries
                ' in the list. This is OK because the UI in Project Property Pages disallows users from
                ' adding multiple entries. Hence the potential first-chance exception here is not a
                ' problem, it should in theory never happen.

                Try
                    Dim import As GlobalImport = Nothing
                    If Not s_importsCache.TryGetValue(wszImport, import) Then
                        import = GlobalImport.Parse(wszImport)
                        s_importsCache(wszImport) = import
                    End If

                    _imports.Add(import)
                Catch ex As ArgumentException
                    'TODO: report error
                End Try

                PushUpdatedGlobalImportsToWorkspace()
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Shadows Sub AddProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.AddProjectReference
            Try
                Dim project = TryCast(pReferencedCompilerProject, VisualBasicProject)

                If project Is Nothing Then
                    ' Hmm, we got a project which isn't from ourselves. That's somewhat odd, and we really can't do anything
                    ' with it.
                    Throw New ArgumentException("Unknown type of IVbCompilerProject.", NameOf(pReferencedCompilerProject))
                End If

                MyBase.AddProjectReference(New ProjectReference(project.Id))
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
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
                Contract.Requires(_buildStatusCallback Is Nothing, "IVbBuildStatusCallback already set")

                _buildStatusCallback = pIVbBuildStatusCallback

                If pIVbBuildStatusCallback IsNot Nothing Then
                    ' While Roslyn doesn't have the concept of a "bound" project, this call signals to the
                    ' project System that it can start the debugger hosting process.
                    ' Work Item#777487 tracks the removal of this concept in Dev14
                    pIVbBuildStatusCallback.ProjectBound()
                End If

                Return 0
            Catch e As Exception When FatalError.Report(e)
                Return 0
            End Try
        End Function

        Public Sub UnadviseBuildStatusCallback(dwCookie As UInteger) Implements IVbCompilerProject.UnadviseBuildStatusCallback
            Contract.Requires(dwCookie = 0, "Bad cookie")

            _buildStatusCallback = Nothing
        End Sub

#End Region

        Public MustOverride Function CreateCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef ppCodeModel As EnvDTE.CodeModel) As Integer Implements IVbCompilerProject.CreateCodeModel
        Public MustOverride Function CreateFileCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef ppFileCodeModel As EnvDTE.FileCodeModel) As Integer Implements IVbCompilerProject.CreateFileCodeModel

        Public Sub DeleteAllImports() Implements IVbCompilerProject.DeleteAllImports
            Try
                _imports.Clear()
                PushUpdatedGlobalImportsToWorkspace()
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Sub DeleteAllResourceReferences() Implements IVbCompilerProject.DeleteAllResourceReferences
            ' TODO: implement
        End Sub

        Public Sub DeleteImport(wszImport As String) Implements IVbCompilerProject.DeleteImport
            Try
                Dim index = _imports.FindIndex(Function(import) import.Clause.ToFullString() = wszImport)
                If index >= 0 Then
                    _imports.RemoveAt(index)
                    PushUpdatedGlobalImportsToWorkspace()
                End If
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Overrides Sub Disconnect() Implements IVbCompilerProject.Disconnect
            Try
                MyBase.Disconnect()
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Function ENCRebuild(in_pProgram As Object, ByRef out_ppUpdate As Object) As Integer Implements IVbCompilerProject.ENCRebuild
            Throw New NotSupportedException()
        End Function

        Public Function GetDefaultReferences(cElements As Integer, ByRef rgbstrReferences() As String, ByVal cActualReferences As IntPtr) As Integer Implements IVbCompilerProject.GetDefaultReferences
            Throw New NotImplementedException()
        End Function

        Public Sub GetEntryPointsList(cItems As Integer, strList() As String, ByVal pcActualItems As IntPtr) Implements IVbCompilerProject.GetEntryPointsList
            Try
                Dim project = VisualStudioWorkspace.CurrentSolution.GetProject(Id)
                Dim compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)

                GetEntryPointsWorker(cItems, strList, pcActualItems, findFormsOnly:=False)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Sub GetEntryPointsWorker(cItems As Integer,
                                               strList() As String,
                                               ByVal pcActualItems As IntPtr,
                                               findFormsOnly As Boolean)
            Dim project = VisualStudioWorkspace.CurrentSolution.GetProject(Id)
            Dim compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)

            ' If called with cItems = 0 and pcActualItems != NULL, GetEntryPointsList returns in pcActualItems the number of items available.
            Dim entryPoints = EntryPointFinder.FindEntryPoints(compilation.Assembly.GlobalNamespace, findFormsOnly:=findFormsOnly)
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
            Try
                MyBase.RemoveFile(wszFileName)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Sub RemoveFileByName(wszPath As String) Implements IVbCompilerProject.RemoveFileByName
            Throw New NotImplementedException()
        End Sub

        Public Shadows Sub RemoveMetaDataReference(wszFileName As String) Implements IVbCompilerProject.RemoveMetaDataReference
            wszFileName = FileUtilities.NormalizeAbsolutePath(wszFileName)

            Try
                ' If this is a reference which was explicitly added and is also a runtime library, leave it
                If _explicitlyAddedDefaultReferences.Remove(wszFileName) Then
                    Return
                End If

                MyBase.RemoveMetadataReference(wszFileName)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Shadows Sub RemoveProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.RemoveProjectReference
            Try
                Dim project = TryCast(pReferencedCompilerProject, VisualBasicProject)

                If project Is Nothing Then
                    ' Hmm, we got a project which isn't from ourselves. That's somewhat odd, and we really can't do anything
                    ' with it.
                    Throw New ArgumentException("Unknown type of IVbCompilerProject.", NameOf(pReferencedCompilerProject))
                End If

                If Not Me.CurrentProjectReferencesContains(project.Id) Then
                    Throw New ArgumentException("Project reference to remove is not referenced by this project.", NameOf(pReferencedCompilerProject))
                End If

                Dim projectReference = GetCurrentProjectReferences().Single(Function(r) r.ProjectId Is project.Id)

                MyBase.RemoveProjectReference(projectReference)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Public Sub RenameDefaultNamespace(bstrDefaultNamespace As String) Implements IVbCompilerProject.RenameDefaultNamespace
            ' TODO: implement
        End Sub

        Public Sub RenameFile(wszOldFileName As String, wszNewFileName As String, itemid As UInteger) Implements IVbCompilerProject.RenameFile
            Try
                ' We treat the rename as a removal of the old file and the addition of a new one.
                RemoveFile(wszOldFileName, itemid)
                AddFile(wszNewFileName, itemid, fAddDuringOpen:=False)
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
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
            _rawOptions = pCompilerOptions

            Try
                UpdateOptions()
            Catch e As Exception When FilterException(e)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Sub

        Protected Overrides Sub UpdateOptions()
            Dim newOptions = New ConvertedVisualBasicProjectOptions(_rawOptions, _compilerHost, _imports, GetStrongNameKeyPaths(), ContainingDirectoryPathOpt, Me.ruleSet, GetParsedCommandLineArguments())

            UpdateRuleSetError(Me.ruleSet)

            If newOptions.CompilationOptions <> _lastOptions.CompilationOptions OrElse
               newOptions.ParseOptions <> _lastOptions.ParseOptions Then

                SetOptions(newOptions.CompilationOptions, newOptions.ParseOptions)
            End If

            ' NOTE: _NOT_ using OrdinalIgnoreCase, even though this is a path. If the user
            ' changes the casing in options, we want that to be reflected in the binary we 
            ' produce, etc.
            If Not newOptions.OutputPath.Equals(_lastOptions.OutputPath, StringComparison.Ordinal) Then
                SetOutputPathAndRelatedData(newOptions.OutputPath)
            End If

            ' Push down the new runtime libraries
            If Not newOptions.RuntimeLibraries.SequenceEqual(_lastOptions.RuntimeLibraries, StringComparer.Ordinal) Then
                For Each oldRuntimeLibrary In _lastOptions.RuntimeLibraries
                    If Not _explicitlyAddedDefaultReferences.Contains(oldRuntimeLibrary) Then
                        MyBase.RemoveMetadataReference(oldRuntimeLibrary)
                    End If
                Next

                _explicitlyAddedDefaultReferences.Clear()

                For Each newRuntimeLibrary In newOptions.RuntimeLibraries
                    newRuntimeLibrary = FileUtilities.NormalizeAbsolutePath(newRuntimeLibrary)

                    ' If we already reference this, just skip it
                    If HasMetadataReference(newRuntimeLibrary) Then
                        _explicitlyAddedDefaultReferences.Add(newRuntimeLibrary)
                    Else
                        MyBase.AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(newRuntimeLibrary, MetadataReferenceProperties.Assembly, hResultForMissingFile:=0)
                    End If
                Next
            End If

            _lastOptions = newOptions
        End Sub

        Protected Overrides Function ParseCommandLineArguments(arguments As IEnumerable(Of String)) As CommandLineArguments
            Return VisualBasicCommandLineParser.Default.Parse(arguments, ContainingDirectoryPathOpt, sdkDirectory:=Nothing)
        End Function

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
            ' These are called by third parties during batch edit scenarios. Historically, this would stop the
            ' background compiler so we wouldn't repeatedly decompile/recompile. For Roslyn, we have nothing to
            ' currently do here. If we have some special "batch" edits we can do to the Workspace API, then we could
            ' consider taking advantage of them here.
        End Sub

        Public Sub FinishEdit() Implements IVbCompilerProject.FinishEdit
            ' Called by third parties to finish batch edit scenarios. See comments in StartEdit for details.
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

        Private Sub PushUpdatedGlobalImportsToWorkspace()
            ' We'll just use the last converted options with the global imports changed. If we don't
            ' have any last options, then we won't push anything down at all. We'll call
            ' SetCompilationOptions later once we get the call through
            ' IVbCompiler.SetCompilerOptions
            If _lastOptions IsNot ConvertedVisualBasicProjectOptions.EmptyOptions Then
                SetOptions(_lastOptions.CompilationOptions.WithGlobalImports(_imports), _lastOptions.ParseOptions)
            End If
        End Sub

#If DEBUG Then
        Public Overrides ReadOnly Property Debug_VBEmbeddedCoreOptionOn As Boolean
            Get
                Return _lastOptions.CompilationOptions.EmbedVbCoreRuntime
            End Get
        End Property
#End If
    End Class
End Namespace
