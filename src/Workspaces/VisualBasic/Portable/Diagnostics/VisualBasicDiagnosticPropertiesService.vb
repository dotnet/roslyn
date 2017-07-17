' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(IDiagnosticPropertiesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDiagnosticPropertiesService
        Inherits AbstractDiagnosticPropertiesService

        Private Shared ReadOnly s_compilation As Compilation = VisualBasicCompilation.Create("empty")

        Protected Overrides Function GetCompilation() As Compilation
            Return s_compilation
        End Function
    End Class
End Namespace
