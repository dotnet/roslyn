' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Diagnostics.Analyzers.BlankLines

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers.BlankLines
    <ExportCodeFixProvider(LanguageNames.VisualBasic)>
    Public Class VisualBasicBlankLinesCodeFixProvider
        Inherits AbstractBlankLinesCodeFixProvider

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.IsKind(SyntaxKind.EndOfLineTrivia)
        End Function
    End Class
End Namespace
