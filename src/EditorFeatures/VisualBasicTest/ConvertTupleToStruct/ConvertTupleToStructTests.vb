' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertTupleToStruct
    Public Class ConvertTupleToStructTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(Workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertTupleToStructCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

#Region "update containing member tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleType() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleTypeNoNames() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](1, 2)
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(1, 2)
    end sub
end class

Friend Structure NewStruct
    Public Item1 As Integer
    Public Item2 As Integer

    Public Sub New(item1 As Integer, item2 As Integer)
        Me.Item1 = item1
        Me.Item2 = item2
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return Item1 = other.Item1 AndAlso
               Item2 = other.Item2
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -1030903623
        hashCode = (hashCode * -1521134295 + Item1.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + Item2.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef item1 As Integer, ByRef item2 As Integer)
        item1 = Me.Item1
        item2 = Me.Item2
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (Integer, Integer)
        Return (value.Item1, value.Item2)
    End Operator

    Public Shared Widening Operator CType(value As (Integer, Integer)) As NewStruct
        Return New NewStruct(value.Item1, value.Item2)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleTypePartialNames() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](1, b:=2)
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public Item1 As Integer
    Public b As Integer

    Public Sub New(item1 As Integer, b As Integer)
        Me.Item1 = item1
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return Item1 = other.Item1 AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 174326978
        hashCode = (hashCode * -1521134295 + Item1.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef item1 As Integer, ByRef b As Integer)
        item1 = Me.Item1
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (Integer, b As Integer)
        Return (value.Item1, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.Item1, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 as [||](a as integer, b as integer) = (a:=1, b:=2)
        dim t2 as (a as integer, b as integer) = (a:=1, b:=2)
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 as {|Rename:NewStruct|} = New NewStruct(a:=1, b:=2)
        dim t2 as NewStruct = New NewStruct(a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType2() As Task
            Dim text = "
class Test
    function Method() as (a as integer, b as integer)
        dim t1 as [||](a as integer, b as integer) = (a:=1, b:=2)
        dim t2 as (a as integer, b as integer) = (a:=1, b:=2)
    end function
end class
"
            Dim expected = "
class Test
    function Method() as NewStruct
        dim t1 as {|Rename:NewStruct|} = New NewStruct(a:=1, b:=2)
        dim t2 as NewStruct = New NewStruct(a:=1, b:=2)
    end function
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType3() As Task
            Dim text = "
class Test
    function Method() as (a as integer, b as integer)
        dim t1 as [||](a as integer, b as integer) = (a:=1, b:=2)
        (b as integer, a as integer) t2 = (b:=1, a:=2)
    end function
end class"
            Dim expected = "
class Test
    function Method() as NewStruct
        dim t1 as {|Rename:NewStruct|} = New NewStruct(a:=1, b:=2)
        (b as integer, a as integer) t2 = (b:=1, a:=2)
    end function
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertFromType4() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 as (a as integer, b as integer) = (a:=1, b:=2)
        dim t2 as [||](a as integer, b as integer) = (a:=1, b:=2)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 as NewStruct = New NewStruct(a:=1, b:=2)
        dim t2 as {|Rename:NewStruct|} = New NewStruct(a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleTypeInNamespace() As Task
            Dim text = "
namespace N
    class Test
        sub Method()
            dim t1 = [||](a:=1, b:=2)
        end sub
    end class
end namespace
"
            Dim expected = "
namespace N
    class Test
        sub Method()
            dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        end sub
    end class

    Friend Structure NewStruct
        Public a As Integer
        Public b As Integer

        Public Sub New(a As Integer, b As Integer)
            Me.a = a
            Me.b = b
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is NewStruct) Then
                Return False
            End If

            Dim other = DirectCast(obj, NewStruct)
            Return a = other.a AndAlso
                   b = other.b
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hashCode As Long = 2118541809
            hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
            hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
            Return hashCode
        End Function

        Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
            a = Me.a
            b = Me.b
        End Sub

        Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
            Return (value.a, value.b)
        End Operator

        Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
            Return New NewStruct(value.a, value.b)
        End Operator
    End Structure
end namespace
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestNonLiteralNames() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=Goo(), b:=Bar())
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(Goo(), Bar())
    end sub
end class

Friend Structure NewStruct
    Public a As Object
    Public b As Object

    Public Sub New(a As Object, b As Object)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return System.Collections.Generic.EqualityComparer(Of Object).Default.Equals(a, other.a) AndAlso
               System.Collections.Generic.EqualityComparer(Of Object).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + System.Collections.Generic.EqualityComparer(Of Object).Default.GetHashCode(a)).GetHashCode()
        hashCode = (hashCode * -1521134295 + System.Collections.Generic.EqualityComparer(Of Object).Default.GetHashCode(b)).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Object, ByRef b As Object)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Object, b As Object)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Object, b As Object)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertSingleTupleTypeWithInferredName() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||](a:=1, b)
    end sub
end class"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New {|Rename:NewStruct|}(a:=1, b)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim t2 = New NewStruct(a:=3, b:=4)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleInstancesInSameMethod_DifferingCase() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
        dim t2 = (A:=3, B:=4)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim t2 = New NewStruct(A:=3, B:=4)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleInstancesAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim t2 = New NewStruct(a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function OnlyConvertMatchingTypesInSameMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||](a:=1, b:=2)
        dim t2 = (a:=3, b)
        dim t3 = (a:=4, b:=5, c:=6)
        dim t4 = (b:=5, a:=6)
    end sub
end class"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim t2 = New NewStruct(a:=3, b)
        dim t3 = (a:=4, b:=5, c:=6)
        dim t4 = (b:=5, a:=6)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        Sub foo(a As Integer, b As Integer)

        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestFixAllMatchesInSingleMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||](a:=1, b:=2)
        dim t2 = (a:=3, b)
        dim t3 = (a:=4, b:=5, c:=6)
        dim t4 = (b:=5, a:=6)
    end sub
end class"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim t2 = New NewStruct(a:=3, b)
        dim t3 = (a:=4, b:=5, c:=6)
        dim t4 = (b:=5, a:=6)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestFixNotAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim t2 = New NewStruct(a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function NotIfReferencesAnonymousTypeInternally() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=new With { .c = 1, .d = 2 })
    end sub
end class"

            Await TestMissingInRegularAndScriptAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod1() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=directcast((a:=1, b:=directcast(nothing, object)), object))
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, directcast(New NewStruct(a:=1, directcast(nothing, object)), object))
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Object

    Public Sub New(a As Integer, b As Object)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               System.Collections.Generic.EqualityComparer(Of Object).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + System.Collections.Generic.EqualityComparer(Of Object).Default.GetHashCode(b)).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Object)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Object)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Object)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod2() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=directcast([||](a:=1, b:=directcast(nothing, object)), object))
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, directcast(New {|Rename:NewStruct|}(a:=1, directcast(nothing, object)), object))
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Object

    Public Sub New(a As Integer, b As Object)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               System.Collections.Generic.EqualityComparer(Of Object).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + System.Collections.Generic.EqualityComparer(Of Object).Default.GetHashCode(b)).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Object)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Object)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Object)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function RenameAnnotationOnStartingPoint() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = [||](a:=3, b:=4)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
        dim t2 = New {|Rename:NewStruct|}(a:=3, b:=4)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function CapturedMethodTypeParameters() As Task
            Dim text = "
imports system.collections.generic

class Test(of X as {structure})
    sub Method(of Y as {class, new})(x as List(of X), y as Y())
        dim t1 = [||](a:=x, b:=y)
    end sub
end class"
            Dim expected = "
imports system.collections.generic

class Test(of X as {structure})
    sub Method(of Y as {class, new})(x as List(of X), y as Y())
        dim t1 = New {|Rename:NewStruct|}(Of X, Y)(x, y)
    end sub
end class

Friend Structure NewStruct(Of X As Structure, Y As {Class, New})
    Public a As List(Of X)
    Public b As Y()

    Public Sub New(a As List(Of X), b() As Y)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct(Of X, Y)) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct(Of X, Y))
        Return EqualityComparer(Of List(Of X)).Default.Equals(a, other.a) AndAlso
               EqualityComparer(Of Y()).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + EqualityComparer(Of List(Of X)).Default.GetHashCode(a)).GetHashCode()
        hashCode = (hashCode * -1521134295 + EqualityComparer(Of Y()).Default.GetHashCode(b)).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As List(Of X), ByRef b() As Y)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct(Of X, Y)) As (a As List(Of X), b As Y())
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As List(Of X), b As Y())) As NewStruct(Of X, Y)
        Return New NewStruct(Of X, Y)(value.a, value.b)
    End Operator
End Structure
"

            Await TestExactActionSetOfferedAsync(text, {
                FeaturesResources.updating_usages_in_containing_member
            })
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function NewTypeNameCollision() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class

class NewStruct
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct1|}(a:=1, b:=2)
    end sub
end class

class NewStruct
end class

Friend Structure NewStruct1
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct1) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct1)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct1) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct1
        Return New NewStruct1(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function NewTypeNameCollision_CaseInsensitive() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class

class newstruct
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct1|}(a:=1, b:=2)
    end sub
end class

class newstruct
end class

Friend Structure NewStruct1
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct1) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct1)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct1) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct1
        Return New NewStruct1(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestDuplicatedName() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, a:=2)
    end sub
end class"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, a:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public a As Integer

    Public Sub New(a As Integer, a As Integer)
        Me.a = a
        Me.a = a
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return Me.a = other.a AndAlso
               Me.a = other.a
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2068208952
        hashCode = (hashCode * -1521134295 + Me.a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + Me.a.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef a As Integer)
        a = Me.a
        a = Me.a
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, a As Integer)
        Return (value.a, value.a)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, a As Integer)) As NewStruct
        Return New NewStruct(value.a, value.a)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestInLambda1() As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
        dim a = sub ()
                    dim t2 = (a:=3, b:=4)
                end sub()
    end sub
end class"
            Dim expected = "
imports System

class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        dim a = sub ()
                    dim t2 = New NewStruct(a:=3, b:=4)
                end sub()
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestInLambda2() As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim a = sub ()
                    dim t2 = [||](a:=3, b:=4)
                end sub()
    end sub
end class"
            Dim expected = "
imports System

class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
        dim a = sub ()
                    dim t2 = New {|Rename:NewStruct|}(a:=3, b:=4)
                end sub()
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertWithDefaultNames1() As Task
            Dim text As String = "
class Test
    sub Method()
        dim t1 = [||](1, 2)
        dim t2 = (1, 2)
        dim t3 = (a:=1, b:=2)
        dim t4 = (Item1:=1, Item2:=2)
        dim t5 = (item1:=1, item2:=2)
    end sub
end class
"
            Dim expected As String = "
class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(1, 2)
        dim t2 = New NewStruct(1, 2)
        dim t3 = (a:=1, b:=2)
        dim t4 = New NewStruct(Item1:=1, Item2:=2)
        dim t5 = New NewStruct(item1:=1, item2:=2)
    end sub
end class

Friend Structure NewStruct
    Public Item1 As Integer
    Public Item2 As Integer

    Public Sub New(item1 As Integer, item2 As Integer)
        Me.Item1 = item1
        Me.Item2 = item2
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return Item1 = other.Item1 AndAlso
               Item2 = other.Item2
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -1030903623
        hashCode = (hashCode * -1521134295 + Item1.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + Item2.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef item1 As Integer, ByRef item2 As Integer)
        item1 = Me.Item1
        item2 = Me.Item2
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (Integer, Integer)
        Return (value.Item1, value.Item2)
    End Operator

    Public Shared Widening Operator CType(value As (Integer, Integer)) As NewStruct
        Return New NewStruct(value.Item1, value.Item2)
    End Operator
End Structure
"
            Await TestExactActionSetOfferedAsync(text, {
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            })

            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function ConvertWithDefaultNames2() As Task
            Dim text As String = "
class Test
    sub Method()
        dim t1 = (1, 2)
        dim t2 = (1, 2)
        dim t3 = (a:=1, b:=2)
        dim t4 = [||](Item1:=1, Item2:=2)
        dim t5 = (item1:=1, item2:=2)
    end sub
end class
"
            Dim expected As String = "
class Test
    sub Method()
        dim t1 = New NewStruct(1, 2)
        dim t2 = New NewStruct(1, 2)
        dim t3 = (a:=1, b:=2)
        dim t4 = New {|Rename:NewStruct|}(Item1:=1, Item2:=2)
        dim t5 = New NewStruct(item1:=1, item2:=2)
    end sub
end class

Friend Structure NewStruct
    Public Item1 As Integer
    Public Item2 As Integer

    Public Sub New(item1 As Integer, item2 As Integer)
        Me.Item1 = item1
        Me.Item2 = item2
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return Item1 = other.Item1 AndAlso
               Item2 = other.Item2
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = -1030903623
        hashCode = (hashCode * -1521134295 + Item1.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + Item2.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef item1 As Integer, ByRef item2 As Integer)
        item1 = Me.Item1
        item2 = Me.Item2
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (Item1 As Integer, Item2 As Integer)
        Return (value.Item1, value.Item2)
    End Operator

    Public Shared Widening Operator CType(value As (Item1 As Integer, Item2 As Integer)) As NewStruct
        Return New NewStruct(value.Item1, value.Item2)
    End Operator
End Structure
"
            Await TestExactActionSetOfferedAsync(text, {
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            })

            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return Nothing
        End Function

#End Region

#Region "update containing type tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function TestCapturedTypeParameter_UpdateType() As Task
            Dim text = "
imports System

class Test(of T)
    sub Method(t as T)
        dim t1 = [||](a:=t, b:=2)
    end sub

    dim t as T
    sub Goo()
        dim t2 = (a:=t, b:=4)
    end sub

    sub Blah(of T)(t as T)
        dim t2 = (a:=t, b:=4)
    end sub
end class"
            Dim expected = "
imports System

class Test(of T)
    sub Method(t as T)
        dim t1 = New {|Rename:NewStruct|}(Of T)(t, b:=2)
    end sub

    dim t as T
    sub Goo()
        dim t2 = New NewStruct(Of T)(t, b:=4)
    end sub

    sub Blah(of T)(t as T)
        dim t2 = (a:=t, b:=4)
    end sub
end class

Friend Structure NewStruct(Of T)
    Public a As T
    Public b As Integer

    Public Sub New(a As T, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct(Of T)) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct(Of T))
        Return Collections.Generic.EqualityComparer(Of T).Default.Equals(a, other.a) AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + Collections.Generic.EqualityComparer(Of T).Default.GetHashCode(a)).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As T, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct(Of T)) As (a As T, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As T, b As Integer)) As NewStruct(Of T)
        Return New NewStruct(Of T)(value.a, value.b)
    End Operator
End Structure
"

            Await TestExactActionSetOfferedAsync(text, {
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            })
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInType_SinglePart_SingleFile() As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub

    sub Goo()
        dim t2 = (a:=3, b:=4)
    end sub
end class
class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class"
            Dim expected = "
imports System

class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
    end sub

    sub Goo()
        dim t2 = New NewStruct(a:=3, b:=4)
    end sub
end class
class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInType_MultiplePart_SingleFile() As Task
            Dim text = "
imports System

partial class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class
partial class Test
    function Goo() as (a as integer, b as integer)
        dim t2 = (a:=3, b:=4)
    end function
end class
class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class"
            Dim expected = "
imports System

partial class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
    end sub
end class
partial class Test
    function Goo() as NewStruct
        dim t2 = New NewStruct(a:=3, b:=4)
    end function
end class
class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode As Long = 2118541809
        hashCode = (hashCode * -1521134295 + a.GetHashCode()).GetHashCode()
        hashCode = (hashCode * -1521134295 + b.GetHashCode()).GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
"
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInType_MultiplePart_MultipleFile() As Task
            Dim text = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class

partial class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
        <Document>
imports System

partial class Test
    function Goo() as (a as integer, b as integer)
        dim t2 = (a:=3, b:=4)
    end function
end class

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"

            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
    end sub
end class

partial class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
</Document>
        <Document>
imports System

partial class Test
    function Goo() as NewStruct
        dim t2 = New NewStruct(a:=3, b:=4)
    end function
end class

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

#End Region

#Region "update containing project tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateAllInProject_MultiplePart_MultipleFile_WithNamespace() As Task
            Dim text = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

namespace N
    partial class Test
        sub Method()
            dim t1 = [||](a:=1, b:=2)
        end sub
    end class

    partial class Other
        sub Method()
            dim t1 = (a:=1, b:=2)
        end sub
    end class
end namespace
        </Document>
        <Document>
imports System

partial class Test
    function Goo() as (a as integer, b as integer)
        dim t2 = (a:=3, b:=4)
    end function
end class

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"

            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

namespace N
    partial class Test
        sub Method()
            dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
        end sub
    end class

    partial class Other
        sub Method()
            dim t1 = New NewStruct(a:=1, b:=2)
        end sub
    end class

    Friend Structure NewStruct
        Public a As Integer
        Public b As Integer

        Public Sub New(a As Integer, b As Integer)
            Me.a = a
            Me.b = b
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is NewStruct) Then
                Return False
            End If

            Dim other = DirectCast(obj, NewStruct)
            Return a = other.a AndAlso
                   b = other.b
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hashCode = 2118541809
            hashCode = hashCode * -1521134295 + a.GetHashCode()
            hashCode = hashCode * -1521134295 + b.GetHashCode()
            Return hashCode
        End Function

        Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
            a = Me.a
            b = Me.b
        End Sub

        Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
            Return (value.a, value.b)
        End Operator

        Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
            Return New NewStruct(value.a, value.b)
        End Operator
    End Structure
end namespace
        </Document>
        <Document>
imports System

partial class Test
    function Goo() as N.NewStruct
        dim t2 = New N.NewStruct(a:=3, b:=4)
    end function
end class

partial class Other
    sub Goo()
        dim t1 = New N.NewStruct(a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=2)
        End Function

#End Region

#Region "update dependent projects"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateDependentProjects_DirectDependency() As Task
            Dim text = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class

partial class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
imports System

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"
            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
    end sub
end class

partial class Other
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
    end sub
end class

Public Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
</Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
imports System

partial class Other
    sub Goo()
        dim t1 = New NewStruct(a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
        Public Async Function UpdateDependentProjects_NoDependency() As Task
            Dim text = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class

partial class Other
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
imports System

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"
            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
imports System

partial class Test
    sub Method()
        dim t1 = New {|Rename:NewStruct|}(a:=1, b:=2)
    end sub
end class

partial class Other
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
    end sub
end class

Public Structure NewStruct
    Public a As Integer
    Public b As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.a = a
        Me.b = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return a = other.a AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Dim hashCode = 2118541809
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        Return hashCode
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.a
        b = Me.b
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (a As Integer, b As Integer)
        Return (value.a, value.b)
    End Operator

    Public Shared Widening Operator CType(value As (a As Integer, b As Integer)) As NewStruct
        Return New NewStruct(value.a, value.b)
    End Operator
End Structure
</Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
imports System

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class
        </Document>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(text, expected, index:=3)
        End Function

#End Region

    End Class
End Namespace
