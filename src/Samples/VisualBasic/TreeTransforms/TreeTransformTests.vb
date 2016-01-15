' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Public Module TreeTransformTests

    Public Function IntTypeToLongTypeTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function TrueToFalseTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function FalseToTrueTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function ClassToStructureTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function StructureToClassTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function OrderByAscToOrderByDescTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function OrderByDescToOrderByAscTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function AddAssignmentToAssignmentTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function DirectCastToTryCastTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function TryCastToDirectCastTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function InitVariablesToNothingTest() As Boolean
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
        Dim x As Integer = Nothing, y As Object = Nothing, d As Decimal = Nothing, m1 = Nothing
    End Sub
End Module
</a>.Value

        Dim actual_transform = Transforms.Transform(input, TransformKind.InitVariablesToNothing)

        Return expected_transform = actual_transform
    End Function

    Public Function ByValParamToByRefParamTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function ByRefParamToByValParamTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function DoTopTestToDoBottomTestTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function DoBottomTestToDoTopTestTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function WhileToDoWhileTopTestTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function DoWhileTopTestToWhileTest() As Boolean
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

        Return expected_transform = actual_transform
    End Function

    Public Function SingleLineIfToMultiLineIfTest() As Boolean
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
"        If True Then " & vbCr & vbLf &
"            A = B + C " & vbCr & vbLf &
"            B = A + C " & vbCr & vbLf &
"        Else" & vbCr & vbLf &
"            C = A + B " & vbCr & vbLf &
"            B = A - C" & vbCr & vbLf &
"        End If" & vbLf &
"    End Sub" & vbLf &
"End Module" & vbLf

        Dim actual_transform = Transforms.Transform(input, TransformKind.SingleLineIfToMultiLineIf)

        Return expected_transform = actual_transform
    End Function

End Module
