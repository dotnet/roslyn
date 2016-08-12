' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.IO

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class BinaryExpressionSyntax
        Protected Overrides Sub WriteTo(writer As TextWriter, leading As Boolean, trailing As Boolean)
            'Do not blow the stack due to a deep recursion on the left. 

            Dim childAsBinary = TryCast(Me.Left, BinaryExpressionSyntax)

            If childAsBinary Is Nothing Then
                MyBase.WriteTo(writer, leading, trailing)
                Return
            End If

            Dim stack = ArrayBuilder(Of BinaryExpressionSyntax).GetInstance()
            stack.Push(Me)

            Dim binary As BinaryExpressionSyntax = childAsBinary
            Dim child As GreenNode = Nothing

            While True
                stack.Push(binary)
                child = binary.Left
                childAsBinary = TryCast(child, BinaryExpressionSyntax)

                If childAsBinary Is Nothing Then
                    Exit While
                End If

                binary = childAsBinary
            End While

            child.WriteTo(writer, leading:=leading, trailing:=True)

            Do
                binary = stack.Pop()

                binary.OperatorToken.WriteTo(writer, leading:=True, trailing:=True)
                binary.Right.WriteTo(writer, leading:=True, trailing:=trailing Or Not ReferenceEquals(binary, Me))
            Loop While (Not ReferenceEquals(binary, Me))

            Debug.Assert(stack.Count = 0)
            stack.Free()
        End Sub
    End Class
End Namespace