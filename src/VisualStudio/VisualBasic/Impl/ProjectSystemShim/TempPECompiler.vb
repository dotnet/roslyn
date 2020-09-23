' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend Class TempPECompiler
        Implements IVbCompiler

        Private ReadOnly _workspace As VisualStudioWorkspace
        Private ReadOnly _projects As New List(Of TempPEProject)

        Public Sub New(workspace As VisualStudioWorkspace)
            Me._workspace = workspace
        End Sub

        Public Function Compile(ByVal pcWarnings As IntPtr, ByVal pcErrors As IntPtr, ByVal ppErrors As IntPtr) As Integer Implements IVbCompiler.Compile
            ' The CVbTempPECompiler never wants errors and simply passes in NULL.
            Contract.ThrowIfFalse(ppErrors = IntPtr.Zero)

            Dim metadataService = _workspace.Services.GetService(Of IMetadataService)
            Dim errors As Integer = 0

            For Each project In _projects
                errors += project.CompileAndGetErrorCount(metadataService)
            Next

            If pcErrors <> IntPtr.Zero Then
                Marshal.WriteInt32(pcErrors, errors)
            End If

            Return VSConstants.S_OK
        End Function

        Public Function CreateProject(wszName As String,
                                      punkProject As Object,
                                      pProjHier As IVsHierarchy,
                                      pVbCompilerHost As IVbCompilerHost) As IVbCompilerProject Implements IVbCompiler.CreateProject
            Dim project = New TempPEProject(pVbCompilerHost)

            _projects.Add(project)

            Return project
        End Function

        Public Function IsValidIdentifier(wszIdentifier As String) As Boolean Implements IVbCompiler.IsValidIdentifier
            Throw New NotImplementedException()
        End Function

        Public Sub RegisterVbCompilerHost(pVbCompilerHost As IVbCompilerHost) Implements IVbCompiler.RegisterVbCompilerHost
            ' The project system registers IVbCompilerHosts with us by calling this method, but
            ' don't care about it in the first place. Thus this is a no-op.
        End Sub

        Public Sub SetDebugSwitches(dbgSwitches() As Boolean) Implements IVbCompiler.SetDebugSwitches
            Throw New NotImplementedException()
        End Sub

        Public Sub SetLoggingOptions(options As UInteger) Implements IVbCompiler.SetLoggingOptions
            Throw New NotImplementedException()
        End Sub

        Public Sub SetOutputLevel(OutputLevel As OutputLevel) Implements IVbCompiler.SetOutputLevel
            Throw New NotImplementedException()
        End Sub

        Public Sub SetWatsonType(WatsonType As WatsonType, WatsonLcid As Integer, wszAdditionalFiles As String) Implements IVbCompiler.SetWatsonType
            Throw New NotImplementedException()
        End Sub

        Public Sub StartBackgroundCompiler() Implements IVbCompiler.StartBackgroundCompiler
            Throw New NotImplementedException()
        End Sub

        Public Sub StopBackgroundCompiler() Implements IVbCompiler.StopBackgroundCompiler
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace
