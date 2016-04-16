' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
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
End Module
