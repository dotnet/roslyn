' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices.ComTypes
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend Class TempPECompiler
        Private Class TempPEProject
            Implements IVbCompilerProject

            Private ReadOnly _compilerHost As IVbCompilerHost
            Private ReadOnly _references As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Private ReadOnly _files As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            Private _parseOptions As VisualBasicParseOptions
            Private _compilationOptions As VisualBasicCompilationOptions
            Private _outputPath As String
            Private _runtimeLibraries As ImmutableArray(Of String)

            Public Sub New(compilerHost As IVbCompilerHost)
                _compilerHost = compilerHost
            End Sub

            Public Function CompileAndGetErrorCount(metadataService As IMetadataService) As Integer
                Dim trees = _files.Select(Function(path)
                                              Using stream = FileUtilities.OpenRead(path)
                                                  Return SyntaxFactory.ParseSyntaxTree(SourceText.From(stream), options:=_parseOptions, path:=path)
                                              End Using
                                          End Function)

                Dim metadataReferences = _references.Concat(_runtimeLibraries) _
                                                      .Distinct(StringComparer.InvariantCultureIgnoreCase) _
                                                      .Select(Function(path) metadataService.GetReference(path, MetadataReferenceProperties.Assembly))

                Dim c = VisualBasicCompilation.Create(
                    Path.GetFileName(_outputPath),
                    trees,
                    metadataReferences,
                    _compilationOptions.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default))

                Dim emitResult = c.Emit(_outputPath)

                Return emitResult.Diagnostics.Where(Function(d) d.Severity = DiagnosticSeverity.Error).Count()
            End Function

            Public Sub AddApplicationObjectVariable(wszClassName As String, wszMemberName As String) Implements IVbCompilerProject.AddApplicationObjectVariable
                Throw New NotImplementedException()
            End Sub

            Public Sub AddBuffer(wszBuffer As String, dwLen As Integer, wszMkr As String, itemid As UInteger, fAdvise As Boolean, fShowErrorsInTaskList As Boolean) Implements IVbCompilerProject.AddBuffer
                Throw New NotImplementedException()
            End Sub

            Public Function AddEmbeddedMetaDataReference(wszFileName As String) As Integer Implements IVbCompilerProject.AddEmbeddedMetaDataReference
                Return VSConstants.E_NOTIMPL
            End Function

            Public Sub AddEmbeddedProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.AddEmbeddedProjectReference
                Throw New NotImplementedException()
            End Sub

            Public Sub AddFile(wszFileName As String, itemid As UInteger, fAddDuringOpen As Boolean) Implements IVbCompilerProject.AddFile
                ' We are only ever given VSITEMIDs that are Nil because a TempPE project isn't
                ' associated with a IVsHierarchy.
                Contract.ThrowIfFalse(itemid = VSConstants.VSITEMID.Nil)

                _files.Add(wszFileName)
            End Sub

            Public Sub AddImport(wszImport As String) Implements IVbCompilerProject.AddImport
                Throw New NotImplementedException()
            End Sub

            Public Function AddMetaDataReference(wszFileName As String, bAssembly As Boolean) As Integer Implements IVbCompilerProject.AddMetaDataReference
                _references.Add(wszFileName)

                Return VSConstants.S_OK
            End Function

            Public Sub AddProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.AddProjectReference
                Throw New NotImplementedException()
            End Sub

            Public Sub AddResourceReference(wszFileName As String, wszName As String, fPublic As Boolean, fEmbed As Boolean) Implements IVbCompilerProject.AddResourceReference
                Throw New NotImplementedException()
            End Sub

            Public Function AdviseBuildStatusCallback(pIVbBuildStatusCallback As IVbBuildStatusCallback) As UInteger Implements IVbCompilerProject.AdviseBuildStatusCallback
                Throw New NotImplementedException()
            End Function

            Public Function CreateCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef pCodeModel As EnvDTE.CodeModel) As Integer Implements IVbCompilerProject.CreateCodeModel
                Throw New NotImplementedException()
            End Function

            Public Function CreateFileCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef pFileCodeModel As EnvDTE.FileCodeModel) As Integer Implements IVbCompilerProject.CreateFileCodeModel
                Throw New NotImplementedException()
            End Function

            Public Sub DeleteAllImports() Implements IVbCompilerProject.DeleteAllImports
                Throw New NotImplementedException()
            End Sub

            Public Sub DeleteAllResourceReferences() Implements IVbCompilerProject.DeleteAllResourceReferences
                Throw New NotImplementedException()
            End Sub

            Public Sub DeleteImport(wszImport As String) Implements IVbCompilerProject.DeleteImport
                Throw New NotImplementedException()
            End Sub

            Public Sub Disconnect() Implements IVbCompilerProject.Disconnect

            End Sub

            Public Function ENCRebuild(in_pProgram As Object, ByRef out_ppUpdate As Object) As Integer Implements IVbCompilerProject.ENCRebuild
                Throw New NotImplementedException()
            End Function

            Public Sub FinishEdit() Implements IVbCompilerProject.FinishEdit
                ' The project system calls BeginEdit/FinishEdit so we can batch and avoid doing
                ' expensive things between each call to one of the Add* methods. But since we're not
                ' doing anything expensive, this can be a no-op.
            End Sub

            Public Function GetDefaultReferences(cElements As Integer, ByRef rgbstrReferences() As String, ByVal cActualReferences As IntPtr) As Integer Implements IVbCompilerProject.GetDefaultReferences
                Throw New NotImplementedException()
            End Function

            Public Sub GetEntryPointsList(cItems As Integer, strList() As String, ByVal pcActualItems As IntPtr) Implements IVbCompilerProject.GetEntryPointsList
                Throw New NotImplementedException()
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

            Public Sub RemoveFile(wszFileName As String, itemid As UInteger) Implements IVbCompilerProject.RemoveFile
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveFileByName(wszPath As String) Implements IVbCompilerProject.RemoveFileByName
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveMetaDataReference(wszFileName As String) Implements IVbCompilerProject.RemoveMetaDataReference
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveProjectReference(pReferencedCompilerProject As IVbCompilerProject) Implements IVbCompilerProject.RemoveProjectReference
                Throw New NotImplementedException()
            End Sub

            Public Sub RenameDefaultNamespace(bstrDefaultNamespace As String) Implements IVbCompilerProject.RenameDefaultNamespace
                Throw New NotImplementedException()
            End Sub

            Public Sub RenameFile(wszOldFileName As String, wszNewFileName As String, itemid As UInteger) Implements IVbCompilerProject.RenameFile
                Throw New NotImplementedException()
            End Sub

            Public Sub RenameProject(wszNewProjectName As String) Implements IVbCompilerProject.RenameProject
                Throw New NotImplementedException()
            End Sub

            Public Sub ResumePostedNotifications() Implements IVbCompilerProject.ResumePostedNotifications
                Throw New NotImplementedException()
            End Sub

            Public Sub SetBackgroundCompilerPriorityLow() Implements IVbCompilerProject.SetBackgroundCompilerPriorityLow
                Throw New NotImplementedException()
            End Sub

            Public Sub SetBackgroundCompilerPriorityNormal() Implements IVbCompilerProject.SetBackgroundCompilerPriorityNormal
                Throw New NotImplementedException()
            End Sub

            Public Sub SetCompilerOptions(ByRef pCompilerOptions As VBCompilerOptions) Implements IVbCompilerProject.SetCompilerOptions
                _runtimeLibraries = VisualBasicProject.OptionsProcessor.GetRuntimeLibraries(_compilerHost, pCompilerOptions)
                _outputPath = PathUtilities.CombinePathsUnchecked(pCompilerOptions.wszOutputPath, pCompilerOptions.wszExeName)
                _parseOptions = VisualBasicProject.OptionsProcessor.ApplyVisualBasicParseOptionsFromCompilerOptions(VisualBasicParseOptions.Default, pCompilerOptions)

                ' Note that we pass a "default" compilation options with DLL set as output kind; the Apply method will figure out what the right one is and fix it up
                _compilationOptions = VisualBasicProject.OptionsProcessor.ApplyCompilationOptionsFromVBCompilerOptions(
                    New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, parseOptions:=_parseOptions), pCompilerOptions)
            End Sub

            Public Sub SetModuleAssemblyName(wszName As String) Implements IVbCompilerProject.SetModuleAssemblyName
                Throw New NotImplementedException()
            End Sub

            Public Sub SetStreamForPDB(pStreamPDB As IStream) Implements IVbCompilerProject.SetStreamForPDB
                Throw New NotImplementedException()
            End Sub

            Public Sub StartBuild(pVsOutputWindowPane As IVsOutputWindowPane, fRebuildAll As Boolean) Implements IVbCompilerProject.StartBuild
                Throw New NotImplementedException()
            End Sub

            Public Sub StartDebugging() Implements IVbCompilerProject.StartDebugging
                Throw New NotImplementedException()
            End Sub

            Public Sub StartEdit() Implements IVbCompilerProject.StartEdit
                ' The project system calls BeginEdit/FinishEdit so we can batch and avoid doing
                ' expensive things between each call to one of the Add* methods. But since we're not
                ' doing anything expensive, this can be a no-op.
            End Sub

            Public Sub StopBuild() Implements IVbCompilerProject.StopBuild
                Throw New NotImplementedException()
            End Sub

            Public Sub StopDebugging() Implements IVbCompilerProject.StopDebugging
                Throw New NotImplementedException()
            End Sub

            Public Sub SuspendPostedNotifications() Implements IVbCompilerProject.SuspendPostedNotifications
                Throw New NotImplementedException()
            End Sub

            Public Sub UnadviseBuildStatusCallback(dwCookie As UInteger) Implements IVbCompilerProject.UnadviseBuildStatusCallback
                Throw New NotImplementedException()
            End Sub

            Public Sub WaitUntilBound() Implements IVbCompilerProject.WaitUntilBound
                Throw New NotImplementedException()
            End Sub

        End Class
    End Class
End Namespace
