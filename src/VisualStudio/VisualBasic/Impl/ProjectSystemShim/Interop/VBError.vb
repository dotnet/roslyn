' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    Friend Structure VBError
        Public dwErrId As UInteger

        <MarshalAs(UnmanagedType.BStr)>
        Public ItemName As String

        <MarshalAs(UnmanagedType.BStr)>
        Public FileName As String

        <MarshalAs(UnmanagedType.BStr)>
        Public Description As String

        <MarshalAs(UnmanagedType.BStr)>
        Public SourceText As String

        Public dwBeginLine As Integer
        Public dwEndLine As Integer
        Public dwBeginCol As Integer
        Public dwEndCol As Integer
        Public dwSeverity As Integer
    End Structure
End Namespace
