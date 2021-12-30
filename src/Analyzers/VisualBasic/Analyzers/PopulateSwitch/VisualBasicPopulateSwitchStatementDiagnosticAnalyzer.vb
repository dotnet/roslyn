' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PopulateSwitch

Namespace Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicPopulateSwitchStatementDiagnosticAnalyzer
        Inherits AbstractPopulateSwitchStatementDiagnosticAnalyzer(Of SelectBlockSyntax)
    End Class
End Namespace
