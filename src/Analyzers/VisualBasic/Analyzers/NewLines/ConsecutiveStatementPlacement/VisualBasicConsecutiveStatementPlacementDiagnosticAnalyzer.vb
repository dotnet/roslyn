' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement

Namespace Microsoft.CodeAnalysis.VisualBasic.NewLines.ConsecutiveStatementPlacement
    ''' <summary>
    ''' Analyzer that finds code of the form:
    ''' <code>
    ''' If cond
    ''' End If
    ''' NextStatement()
    ''' </code>
    ''' 
    ''' And requires it to be of the form:
    ''' <code>
    ''' If cond
    ''' End If
    ''' 
    ''' NextStatement()
    ''' </code>
    ''' 
    ''' Specifically, all blocks followed by another statement must have a blank line between them.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConsecutiveStatementPlacementDiagnosticAnalyzer
        Inherits AbstractConsecutiveStatementPlacementDiagnosticAnalyzer(Of ExecutableStatementSyntax)

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.EndOfLineTrivia
        End Function

        Protected Overrides Function IsWhitespace(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind() = SyntaxKind.WhitespaceTrivia
        End Function

        Protected Overrides Function IsBlockStatement(node As SyntaxNode) As Boolean
            Return TypeOf node Is EndBlockStatementSyntax OrElse TypeOf node Is NextStatementSyntax
        End Function

        Protected Overrides Function GetDiagnosticLocation(block As SyntaxNode) As Location
            Return block.GetLocation()
        End Function
    End Class
End Namespace
