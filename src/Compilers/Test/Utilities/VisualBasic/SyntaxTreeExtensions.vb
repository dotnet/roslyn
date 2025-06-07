' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Public Module SyntaxTreeExtensions
    <System.Runtime.CompilerServices.Extension()>
    Public Function WithReplace(syntaxTree As SyntaxTree, offset As Integer, length As Integer, newText As String) As SyntaxTree
        Dim oldFullText = syntaxTree.GetText()
        Dim newFullText = oldFullText.WithChanges(New TextChange(New TextSpan(offset, length), newText))
        Return syntaxTree.WithChangedText(newFullText)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Friend Function WithReplaceFirst(syntaxTree As SyntaxTree, oldText As String, newText As String) As SyntaxTree
        Dim oldFullText = syntaxTree.GetText().ToString()
        Dim offset As Integer = oldFullText.IndexOf(oldText, StringComparison.Ordinal)
        Dim length As Integer = oldText.Length
        Return WithReplace(syntaxTree, offset, length, newText)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function WithReplace(syntaxTree As SyntaxTree, startIndex As Integer, oldText As String, newText As String) As SyntaxTree
        Dim oldFullText = syntaxTree.GetText().ToString()
        Dim offset As Integer = oldFullText.IndexOf(oldText, startIndex, StringComparison.Ordinal)
        Dim length As Integer = oldText.Length
        Return WithReplace(syntaxTree, offset, length, newText)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function WithInsertAt(syntaxTree As SyntaxTree, offset As Integer, newText As String) As SyntaxTree
        Return WithReplace(syntaxTree, offset, 0, newText)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function WithInsertBefore(syntaxTree As SyntaxTree, existingText As String, newText As String) As SyntaxTree
        Dim oldFullText = syntaxTree.GetText().ToString()
        Dim offset As Integer = oldFullText.IndexOf(existingText, StringComparison.Ordinal)
        Return WithReplace(syntaxTree, offset, 0, newText)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function WithRemoveAt(syntaxTree As SyntaxTree, offset As Integer, length As Integer) As SyntaxTree
        Return WithReplace(syntaxTree, offset, length, String.Empty)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Public Function WithRemoveFirst(syntaxTree As SyntaxTree, oldText As String) As SyntaxTree
        Return WithReplaceFirst(syntaxTree, oldText, String.Empty)
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Friend Function Dump(node As SyntaxNode) As String
        Dim visitor = New VisualBasicSyntaxPrinter()
        visitor.Visit(node)
        Return visitor.Dump()
    End Function

    <System.Runtime.CompilerServices.Extension()>
    Friend Function Dump(node As SyntaxTree) As String
        Return node.GetRoot().Dump()
    End Function

    Private Class VisualBasicSyntaxPrinter
        Inherits VisualBasicSyntaxWalker

        ReadOnly Dim builder As PooledStringBuilder
        Dim indent As Integer = 0

        Sub New()
            builder = PooledStringBuilder.GetInstance()
        End Sub

        Function Dump() As String
            Return builder.ToStringAndFree()
        End Function

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            builder.Builder.Append(" "c, repeatCount:=indent)
            builder.Builder.Append(node.Kind.ToString())
            If node.IsMissing Then
                builder.Builder.Append($" (missing)")
            End If
            builder.Builder.AppendLine()

            indent += 2
            MyBase.DefaultVisit(node)
            indent -= 2
        End Sub
    End Class
End Module
