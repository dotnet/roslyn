' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Diagnostics.Analyzers.BlankLines

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers.BlankLines
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class VisualbasicBlankLinesDiagnosticAnalyzer
        Inherits AbstractBlankLinesDiagnosticAnalyzer

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.IsKind(SyntaxKind.EndOfLineTrivia)
        End Function
    End Class
End Namespace
