' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <Guid("782CB503-84B1-4b8f-9AAD-A12B75905015"), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVbCompilerHost
        ''' <summary>
        ''' Output a string to the standard output (console, file, pane, etc.)
        ''' </summary>
        Sub OutputString(<MarshalAs(UnmanagedType.LPWStr)> [string] As String)

        ''' <summary>
        ''' Returns the system SDK directory, where mscorlib.dll and Microsoft.VisualBasic.dll is
        ''' located.
        ''' </summary>
        <PreserveSig>
        Function GetSdkPath(<MarshalAs(UnmanagedType.BStr), Out> ByRef sdkPath As String) As Integer

        ''' <summary>
        ''' Get the target library type.
        ''' </summary>
        Function GetTargetLibraryType() As VBTargetLibraryType
    End Interface
End Namespace
