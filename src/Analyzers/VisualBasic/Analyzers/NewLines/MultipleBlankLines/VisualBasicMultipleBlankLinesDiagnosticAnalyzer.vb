' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.NewLines.MultipleBlankLines
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.NewLines.MultipleBlankLines
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMultipleBlankLinesDiagnosticAnalyzer
        Inherits AbstractMultipleBlankLinesDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
        End Sub
    End Class
End Namespace
