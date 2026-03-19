' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SyntaxTriviaListExtensions

        <Extension()>
        Public Function ContainsPreprocessorDirective(list As SyntaxTriviaList) As Boolean
            Return list.Any(Function(t) t.HasStructure AndAlso TypeOf t.GetStructure() Is DirectiveTriviaSyntax)
        End Function

        <Extension()>
        Public Function WithoutLeadingWhitespaceOrEndOfLine(list As IEnumerable(Of SyntaxTrivia)) As SyntaxTriviaList
            Return list.SkipWhile(Function(t) t.IsWhitespaceOrEndOfLine()).ToSyntaxTriviaList()
        End Function
    End Module
End Namespace
