' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
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
