' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure WarningItemLevel
        Public WarningId As Integer
        Public WarningLevel As WarningLevel
    End Structure
End Namespace
