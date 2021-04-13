' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Linq.Expressions

Structure S1
    Public Shared Operator And(x As S1?, y As S1?) As S1?
        Return x
    End Operator

    Public Shared Operator Or(x As S1, y As S1) As S1
        Return x
    End Operator

    Public Shared Operator IsFalse(x As S1?) As Boolean
        Return False
    End Operator

    Public Shared Operator IsTrue(x As S1?) As Boolean
        Return True
    End Operator

    Public Shared Operator IsFalse(x As S1) As Boolean
        Return False
    End Operator

    Public Shared Operator IsTrue(x As S1) As Boolean
        Return True
    End Operator
End Structure

Structure S2
    Public Shared Operator And(x As S2, y As S2) As S2
        Return x
    End Operator

    Public Shared Operator Or(x As S2, y As S2) As S2
        Return x
    End Operator

    Public Shared Operator IsFalse(x As S2) As Boolean
        Return False
    End Operator

    Public Shared Operator IsTrue(x As S2) As Boolean
        Return True
    End Operator
End Structure

Class O1
    Public Shared Operator And(x As O1, y As O1) As O1
        Return x
    End Operator

    Public Shared Operator Or(x As O1, y As O1) As O1
        Return y
    End Operator

    Public Shared Operator IsFalse(x As O1) As Boolean
        Return False
    End Operator

    Public Shared Operator IsTrue(x As O1) As Boolean
        Return True
    End Operator
End Class

Public Class TestClass
    Public Sub Test()
    End Sub
End Class

Module Form1
    Sub Main()
        Dim ret1 As Expression(Of Func(Of S1, S1?, Object)) = Function(x, y) If((x AndAlso y) And (y AndAlso x) And (y AndAlso y) And (x AndAlso x), x, y)
        Console.WriteLine(ret1.Dump)
        Dim ret2 As Expression(Of Func(Of S1, S1?, Object)) = Function(x, y) If((x OrElse y) And (y OrElse x) And (y OrElse y) And (x OrElse x), x, y)
        Console.WriteLine(ret2.Dump)
        Dim ret3 As Expression(Of Func(Of S1, S1?, Object)) = Function(x, y) If(x, x, y) AndAlso If(y, x, y)
        Console.WriteLine(ret3.Dump)

        Dim ret1n As Expression(Of Func(Of S2, S2?, Object)) = Function(x, y) If((x AndAlso y) And (y AndAlso x) And (y AndAlso y) And (x AndAlso x), x, y)
        Console.WriteLine(ret1n.Dump)
        Dim ret2n As Expression(Of Func(Of S2, S2?, Object)) = Function(x, y) If((x OrElse y) And (y OrElse x) And (y OrElse y) And (x OrElse x), x, y)
        Console.WriteLine(ret2n.Dump)
        Dim ret3n As Expression(Of Func(Of S2, S2?, Object)) = Function(x, y) If(x, x, y) AndAlso If(y, x, y)
        Console.WriteLine(ret3n.Dump)

        Dim ret4 As Expression(Of Func(Of O1, O1, Object)) = Function(x, y) If(x AndAlso y, x, y)
        Console.WriteLine(ret4.Dump)
        Dim ret5 As Expression(Of Func(Of O1, O1, Object)) = Function(x, y) If(x OrElse y, x, y)
        Console.WriteLine(ret5.Dump)
        Dim ret6 As Expression(Of Func(Of O1, O1, Object)) = Function(x, y) If(x, x, y)
        Console.WriteLine(ret6.Dump)
    End Sub
End Module

