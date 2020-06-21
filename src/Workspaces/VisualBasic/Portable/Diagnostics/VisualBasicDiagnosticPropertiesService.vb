' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(IDiagnosticPropertiesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDiagnosticPropertiesService
        Inherits AbstractDiagnosticPropertiesService

        Private Shared ReadOnly s_compilation As Compilation = VisualBasicCompilation.Create("empty")

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetCompilation() As Compilation
            Return s_compilation
        End Function
    End Class
End Namespace
