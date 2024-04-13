' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConvertToInterpolatedStringDiagnosticAnalyzer
        Inherits AbstractConvertToInterpolatedStringDiagnosticAnalyzer

        Private Shared ReadOnly s_title As String = VisualBasicAnalyzersResources.Convert_to_interpolated_string

        Public Sub New()
            MyBase.New(s_title)
        End Sub
    End Class
End Namespace
