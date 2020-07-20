' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <Guid("8DF9750D-069B-4B81-973A-152E97420C5C"), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVbTempPECompilerFactory
        Function CreateCompiler() As IVbCompiler
    End Interface
End Namespace
