' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
    Friend Class MockCompilerHost
        Implements IVbCompilerHost

        Private ReadOnly _sdkPath As String

        Public Sub New(sdkPath As String)
            _sdkPath = sdkPath
        End Sub

        Public Shared ReadOnly Property FullFrameworkCompilerHost As MockCompilerHost
            Get
                Return New MockCompilerHost("Z:\FullFramework")
            End Get
        End Property

        Public Shared ReadOnly Property NoSdkCompilerHost As MockCompilerHost
            Get
                Return New MockCompilerHost("")
            End Get
        End Property

        Public Function GetWellKnownDllName(fileName As String) As String
            Return Path.Combine(_sdkPath, fileName)
        End Function

        Public Sub OutputString(<MarshalAs(UnmanagedType.LPWStr)> [string] As String) Implements IVbCompilerHost.OutputString
            Throw New NotImplementedException()
        End Sub

        Public Function GetSdkPath(ByRef sdkPath As String) As Integer Implements IVbCompilerHost.GetSdkPath
            sdkPath = _sdkPath

            If String.IsNullOrEmpty(sdkPath) Then
                Return VSConstants.E_NOTIMPL
            Else
                Return VSConstants.S_OK
            End If
        End Function

        Public Function GetTargetLibraryType() As VBTargetLibraryType Implements IVbCompilerHost.GetTargetLibraryType
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
