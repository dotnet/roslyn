﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

'TODO - This is copied from C# and should be moved to common assemble.
Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module FunctionExtensions

        <Extension()>
        Public Function TransitiveClosure(Of T)(relation As Func(Of T, IEnumerable(Of T)), item As T) As HashSet(Of T)
            Dim closure = New HashSet(Of T)()
            Dim stack = New Stack(Of T)()
            stack.Push(item)
            While stack.Count > 0
                Dim current As T = stack.Pop()
                For Each newItem In relation(current)
                    If closure.Add(newItem) Then
                        stack.Push(newItem)
                    End If
                Next
            End While
            Return closure
        End Function

        <Extension()>
        Public Function ToLanguageSpecific(predicate As Func(Of SyntaxToken, Boolean)) As Func(Of SyntaxToken, Boolean)
            If (predicate = SyntaxToken.Any) Then
                Return SyntaxToken.Any
            ElseIf (predicate = SyntaxToken.NonZeroWidth) Then
                Return SyntaxToken.NonZeroWidth
            End If

            If predicate IsNot Nothing Then
                Return Function(t) predicate(CType(t, SyntaxToken))
            Else
                Return Nothing
            End If
        End Function

        <Extension()>
        Public Function ToLanguageSpecific(predicate As Func(Of SyntaxTrivia, Boolean)) As Func(Of SyntaxTrivia, Boolean)
            If predicate IsNot Nothing Then
                Return Function(t) predicate(CType(t, SyntaxTrivia))
            Else
                Return Nothing
            End If
        End Function

    End Module
End Namespace

