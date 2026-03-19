' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    Partial Friend Class VisualBasicPackage
        Implements IVbCompiler

        Public Function Compile(ByVal pcWarnings As IntPtr, ByVal pcErrors As IntPtr, ByVal ppErrors As IntPtr) As Integer Implements IVbCompiler.Compile
            ' This operation should never be called through the normal project system
            Throw New NotSupportedException()
        End Function

        Public Function CreateProject(wszName As String, punkProject As Object, pProjHier As IVsHierarchy, pVbCompilerHost As IVbCompilerHost) As IVbCompilerProject Implements IVbCompiler.CreateProject
            Return New VisualBasicProject(
                wszName,
                pVbCompilerHost,
                pProjHier,
                TypeOf punkProject Is IVsIntellisenseProject,
                Me,
                ComponentModel.GetService(Of IThreadingContext))
        End Function

        Public Function IsValidIdentifier(wszIdentifier As String) As Boolean Implements IVbCompiler.IsValidIdentifier
            ' This curious function requires us to return whether the given identifier is in fact a valid identifier.
            Dim token = SyntaxFactory.ParseToken(wszIdentifier)
            Return token.Kind = SyntaxKind.IdentifierToken
        End Function

        Public Sub RegisterVbCompilerHost(pVbCompilerHost As IVbCompilerHost) Implements IVbCompiler.RegisterVbCompilerHost
            ' Roslyn has no concept of compiler hosts at this point.
        End Sub

        Public Sub SetDebugSwitches(dbgSwitches() As Boolean) Implements IVbCompiler.SetDebugSwitches
            ' Debug switches don't exist in Roslyn.
            Throw New NotSupportedException()
        End Sub

        Public Sub SetLoggingOptions(options As UInteger) Implements IVbCompiler.SetLoggingOptions

        End Sub

        Public Sub SetOutputLevel(OutputLevel As OutputLevel) Implements IVbCompiler.SetOutputLevel
        End Sub

        Public Sub SetWatsonType(WatsonType As WatsonType, WatsonLcid As Integer, wszAdditionalFiles As String) Implements IVbCompiler.SetWatsonType
            ' TODO: It's not clear what the compiler should do with Watson information at this point.
            Throw New NotImplementedException()
        End Sub

        Public Sub StartBackgroundCompiler() Implements IVbCompiler.StartBackgroundCompiler
            ' This method has no meaning in the Roslyn world. We shall do nothing, since throwing an
            ' exception here will cause all sorts of evil things to happen.
        End Sub

        Public Sub StopBackgroundCompiler() Implements IVbCompiler.StopBackgroundCompiler
            ' This method has no meaning in the Roslyn world. We shall do nothing, since throwing an
            ' exception here will cause all sorts of evil things to happen.
        End Sub
    End Class
End Namespace
