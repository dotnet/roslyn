' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure WarningItemLevel
        Public WarningId As Integer
        Public WarningLevel As WarningLevel
    End Structure
End Namespace
