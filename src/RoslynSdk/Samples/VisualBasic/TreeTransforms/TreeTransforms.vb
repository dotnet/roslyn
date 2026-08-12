' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Imports Xunit

Public Class TreeTransformTests

    <Fact>
    Public Sub IntTypeToLongTypeTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim l1 As List(Of Integer) = New List(Of Integer)
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim x As Long = 10
        Dim l1 As List(Of Long) = New List(Of Long)
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.IntTypeToLongType)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub TrueToFalseTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim b As Boolean = True
        If True Then
        End If
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim b As Boolean = False
        If False Then
        End If
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.TrueToFalse)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub FalseToTrueTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim b As Boolean = False
        If False Then
        End If
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim b As Boolean = True
        If True Then
        End If
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.FalseToTrue)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub ClassToStructureTest()
        Dim input As String =
<a>
Class Test
    Sub Main()
    End Sub
End Class
</a>.Value

        Dim expected_transform =
<a>
Structure Test
    Sub Main()
    End Sub
End Structure
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.ClassToStructure)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub StructureToClassTest()
        Dim input As String =
<a>
Structure Test
    Sub Main()
    End Sub
End Structure
</a>.Value

        Dim expected_transform =
<a>
Class Test
    Sub Main()
    End Sub
End Class
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.StructureToClass)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub OrderByAscToOrderByDescTest()
        Dim input As String =
<a>
Imports System.Linq

Module Module1
    Sub Main()
        Dim numbers() = {3, 1, 4, 6, 10}

        Dim sortedNumbers = From number In numbers
                            Order By number Ascending
                            Select number

        For Each number In sortedNumbers
            System.Console.WriteLine(number)
        Next

    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Imports System.Linq

Module Module1
    Sub Main()
        Dim numbers() = {3, 1, 4, 6, 10}

        Dim sortedNumbers = From number In numbers
                            Order By number Descending
                            Select number

        For Each number In sortedNumbers
            System.Console.WriteLine(number)
        Next

    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.OrderByAscToOrderByDesc)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub OrderByDescToOrderByAscTest()
        Dim input As String =
<a>
Imports System.Linq

Module Module1
    Sub Main()
        Dim numbers() = {3, 1, 4, 6, 10}

        Dim sortedNumbers = From number In numbers
                            Order By number Descending
                            Select number

        For Each number In sortedNumbers
            System.Console.WriteLine(number)
        Next

    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Imports System.Linq

Module Module1
    Sub Main()
        Dim numbers() = {3, 1, 4, 6, 10}

        Dim sortedNumbers = From number In numbers
                            Order By number Ascending
                            Select number

        For Each number In sortedNumbers
            System.Console.WriteLine(number)
        Next

    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.OrderByDescToOrderByAsc)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub AddAssignmentToAssignmentTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim y As Integer = 20

        x += y

    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim y As Integer = 20

        x = x + y

    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.AddAssignmentToAssignment)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub DirectCastToTryCastTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim y = DirectCast(x, Object)
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim y = TryCast(x, Object)
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.DirectCastToTryCast)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub TryCastToDirectCastTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim y = TryCast(x, Object)
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = 10
        Dim y = DirectCast(x, Object)
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.TryCastToDirectCast)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub InitVariablesToNothingTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim x As Integer, y As Object, d As Decimal, m1
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim x As Integer = Nothing, y As Object = Nothing, d As Decimal = Nothing, m1
 = Nothing    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.InitVariablesToNothing)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub ByValParamToByRefParamTest()
        Dim input As String =
<a>
Module Module1
    Sub Method1(ByVal param1 As Integer, ByRef param2 As Single, ByVal param3 As Decimal)

    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Method1(ByRef param1 As Integer, ByRef param2 As Single, ByRef param3 As Decimal)

    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.ByValParamToByRefParam)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub ByRefParamToByValParamTest()
        Dim input As String =
<a>
Module Module1
    Sub Method1(ByVal param1 As Integer, ByRef param2 As Single, ByVal param3 As Decimal)

    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Method1(ByVal param1 As Integer, ByVal param2 As Single, ByVal param3 As Decimal)

    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.ByRefParamToByValParam)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub DoTopTestToDoBottomTestTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        Do While condition
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
            End If
        Loop
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        Do
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
            End If
        Loop While condition
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.DoTopTestToDoBottomTest)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub DoBottomTestToDoTopTestTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        Do
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
            End If
        Loop While condition
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        Do While condition
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
            End If
        Loop
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.DoBottomTestToDoTopTest)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub WhileToDoWhileTopTestTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        While condition
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
                Exit While
            End If
        End While
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        Do While condition
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
                Exit Do
            End If
        Loop
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.WhileToDoWhileTopTest)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub DoWhileTopTestToWhileTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        Do While condition
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
                Exit Do
            End If
        Loop
    End Sub
End Module
</a>.Value

        Dim expected_transform =
<a>
Module Module1
    Sub Main()
        Dim index As Integer = 0
        Dim condition As Boolean = True

        While condition
            Console.WriteLine(index)
            index += 1
            If (index = 10) Then
                condition = False
                Exit While
            End If
        End While
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.DoWhileTopTestToWhile)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

    <Fact>
    Public Sub SingleLineIfToMultiLineIfTest()
        Dim input As String =
<a>
Module Module1
    Sub Main()
        Dim A, B, C
        If True Then A = B + C : B = A + C Else C = A + B : B = A - C
    End Sub
End Module
</a>.Value

        '        Dim expected_transform =
        '<a>
        'Module Module1
        '    Sub Main()
        '        Dim A, B, C
        '        If True Then 
        '            A = B + C  
        '            B = A + C  
        '        Else 
        '            C = A + B  
        '            B = A - C 
        '        End If
        '    End Sub
        'End Module
        '</a>.Value
        Dim expected_transform = vbLf &
"Module Module1" & vbLf &
"    Sub Main()" & vbLf &
"        Dim A, B, C" & vbLf &
"        If True Then             A = B + C :            B = A + C         Else            C = A + B :            B = A - C" & vbLf &
"        End If    End Sub" & vbLf &
"End Module" & vbLf

        Dim actual_transform = Transforms.Transform(input, TransformKind.SingleLineIfToMultiLineIf)

        Assert.Equal(expected_transform, actual_transform)
    End Sub

End Class
