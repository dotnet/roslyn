' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.QualifyMemberAccess
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeofToNameof
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConvertTypeofToNameofDiagnosticAnalyzer
        Inherits AbstractConvertTypeofToNameofDiagnosticAnalyzer(Of SyntaxKind, ExpressionSyntax, SimpleNameSyntax)

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function

    End Class
End Namespace
