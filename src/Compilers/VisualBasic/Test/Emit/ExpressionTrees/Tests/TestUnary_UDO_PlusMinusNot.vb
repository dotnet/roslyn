' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Linq.Expressions

Structure S1
    Public Shared Operator +(x As S1?) As S1?
        Return x
    End Operator

    Public Shared Operator -(x As S1?) As S1?
        Return x
    End Operator

    Public Shared Operator Not(x As S1?) As S1?
        Return x
    End Operator
End Structure

Structure S2
    Public Shared Operator +(x As S2) As S2
        Return x
    End Operator

    Public Shared Operator -(x As S2) As S2
        Return x
    End Operator

    Public Shared Operator Not(x As S2) As S2
        Return x
    End Operator
End Structure

Class O1
    Public Shared Operator +(x As O1) As O1
        Return x
    End Operator

    Public Shared Operator -(x As O1) As O1
        Return x
    End Operator

    Public Shared Operator Not(x As O1) As O1
        Return x
    End Operator
End Class

Module Form1
    Sub Main()
        Dim retS1_1 As Expression(Of Func(Of S1, S1)) = Function(x) +x
        Console.WriteLine(retS1_1.Dump)
        Dim retS1_2 As Expression(Of Func(Of S1?, S1)) = Function(x) +x
        Console.WriteLine(retS1_2.Dump)
        Dim retS1_3 As Expression(Of Func(Of S1, S1?)) = Function(x) +x
        Console.WriteLine(retS1_3.Dump)
        Dim retS1_4 As Expression(Of Func(Of S1?, S1?)) = Function(x) +x
        Console.WriteLine(retS1_4.Dump)
        Dim retS1_5 As Expression(Of Func(Of S1, S1)) = Function(x) -x
        Console.WriteLine(retS1_5.Dump)
        Dim retS1_6 As Expression(Of Func(Of S1?, S1)) = Function(x) -x
        Console.WriteLine(retS1_6.Dump)
        Dim retS1_7 As Expression(Of Func(Of S1, S1?)) = Function(x) -x
        Console.WriteLine(retS1_7.Dump)
        Dim retS1_8 As Expression(Of Func(Of S1?, S1?)) = Function(x) -x
        Console.WriteLine(retS1_8.Dump)
        Dim retS1_9 As Expression(Of Func(Of S1, S1)) = Function(x) Not x
        Console.WriteLine(retS1_9.Dump)
        Dim retS1_10 As Expression(Of Func(Of S1?, S1)) = Function(x) Not x
        Console.WriteLine(retS1_10.Dump)
        Dim retS1_11 As Expression(Of Func(Of S1, S1?)) = Function(x) Not x
        Console.WriteLine(retS1_11.Dump)
        Dim retS1_12 As Expression(Of Func(Of S1?, S1?)) = Function(x) Not x
        Console.WriteLine(retS1_12.Dump)

        Dim retS2_1 As Expression(Of Func(Of S2, S2)) = Function(x) +x
        Console.WriteLine(retS2_1.Dump)
        Dim retS2_2 As Expression(Of Func(Of S2?, S2)) = Function(x) +x
        Console.WriteLine(retS2_2.Dump)
        Dim retS2_3 As Expression(Of Func(Of S2, S2?)) = Function(x) +x
        Console.WriteLine(retS2_3.Dump)
        Dim retS2_4 As Expression(Of Func(Of S2?, S2?)) = Function(x) +x
        Console.WriteLine(retS2_4.Dump)
        Dim retS2_5 As Expression(Of Func(Of S2, S2)) = Function(x) -x
        Console.WriteLine(retS2_5.Dump)
        Dim retS2_6 As Expression(Of Func(Of S2?, S2)) = Function(x) -x
        Console.WriteLine(retS2_6.Dump)
        Dim retS2_7 As Expression(Of Func(Of S2, S2?)) = Function(x) -x
        Console.WriteLine(retS2_7.Dump)
        Dim retS2_8 As Expression(Of Func(Of S2?, S2?)) = Function(x) -x
        Console.WriteLine(retS2_8.Dump)
        Dim retS2_9 As Expression(Of Func(Of S2, S2)) = Function(x) Not x
        Console.WriteLine(retS2_9.Dump)
        Dim retS2_10 As Expression(Of Func(Of S2?, S2)) = Function(x) Not x
        Console.WriteLine(retS2_10.Dump)
        Dim retS2_11 As Expression(Of Func(Of S2, S2?)) = Function(x) Not x
        Console.WriteLine(retS2_11.Dump)
        Dim retS2_12 As Expression(Of Func(Of S2?, S2?)) = Function(x) Not x
        Console.WriteLine(retS2_12.Dump)

        Dim retO1_1 As Expression(Of Func(Of O1, O1)) = Function(x) +x
        Console.WriteLine(retO1_1.Dump)
        Dim retO1_5 As Expression(Of Func(Of O1, O1)) = Function(x) -x
        Console.WriteLine(retO1_5.Dump)
        Dim retO1_9 As Expression(Of Func(Of O1, O1)) = Function(x) Not x

    End Sub
End Module
