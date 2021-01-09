' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.NewLines.MultipleBlankLines

Namespace Microsoft.CodeAnalysis.VisualBasic.NewLines.MultipleBlankLines
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMultipleBlankLinesDiagnosticAnalyzer
        Inherits AbstractMultipleBlankLinesDiagnosticAnalyzer

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.IsKind(SyntaxKind.EndOfLineTrivia)
        End Function
    End Class
End Namespace
