' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertTupleToStruct
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct.VisualBasicConvertTupleToStructCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertTupleToStruct
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertTupleToStruct)>
    Public Class ConvertTupleToStructTests

        Private Shared Async Function TestAsync(
            text As String,
            expected As String,
            Optional index As Integer = 0,
            Optional equivalenceKey As String = Nothing,
            Optional testHost As TestHost = TestHost.InProcess,
            Optional actions As String() = Nothing) As Task

            If index <> 0 Then
                Assert.NotNull(equivalenceKey)
            End If

            Dim test = New VerifyVB.Test With {
                .TestCode = text,
                .FixedCode = expected,
                .TestHost = testHost,
                .CodeActionIndex = index,
                .CodeActionEquivalenceKey = equivalenceKey,
                .ExactActionSetOffered = actions,
                .CodeActionValidationMode = Testing.CodeActionValidationMode.None
            }
            Await test.RunAsync()
        End Function

#Region "update containing member tests"

        <Theory, CombinatorialData>
        Public Async Function ConvertSingleTupleType(host As TestHost) As Task
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/45451")>
        Public Async Function ConvertSingleTupleType_ChangeArgumentNameCase(host As TestHost) As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](A:=1, B:=2)
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
    end sub
end class

Friend Structure NewStruct
    Public A As Integer
    Public B As Integer

    Public Sub New(a As Integer, b As Integer)
        Me.A = a
        Me.B = b
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        If Not (TypeOf obj Is NewStruct) Then
            Return False
        End If

        Dim other = DirectCast(obj, NewStruct)
        Return A = other.A AndAlso
               B = other.B
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (A, B).GetHashCode()
    End Function

    Public Sub Deconstruct(ByRef a As Integer, ByRef b As Integer)
        a = Me.A
        b = Me.B
    End Sub

    Public Shared Widening Operator CType(value As NewStruct) As (A As Integer, B As Integer)
        Return (value.A, value.B)
    End Operator

    Public Shared Widening Operator CType(value As (A As Integer, B As Integer)) As NewStruct
        Return New NewStruct(value.A, value.B)
    End Operator
End Structure
"
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertSingleTupleTypeNoNames(host As TestHost) As Task
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
        dim t1 = New NewStruct(1, 2)
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
        Return (Item1, Item2).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertSingleTupleTypePartialNames(host As TestHost) As Task
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
        dim t1 = New NewStruct(1, b:=2)
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
        Return (Item1, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertFromType(host As TestHost) As Task
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
        dim t1 as NewStruct = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertFromType2(host As TestHost) As Task
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
        dim t1 as NewStruct = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertFromType3(host As TestHost) As Task
            Dim text = "
class Test
    function Method() as (a as integer, b as integer)
        dim t1 as [||](a as integer, b as integer) = (a:=1, b:=2)
        dim t2 as (b as integer, a as integer) = (b:=1, a:=2)
    end function
end class"
            Dim expected = "
class Test
    function Method() as NewStruct
        dim t1 as NewStruct = New NewStruct(a:=1, b:=2)
        dim t2 as (b as integer, a as integer) = (b:=1, a:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertFromType4(host As TestHost) As Task
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertSingleTupleTypeInNamespace(host As TestHost) As Task
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
            Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestNonLiteralNames(host As TestHost) As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=Goo(), b:=Bar())
    end sub

    function goo() as object
    end function

    function bar() as object
    end function
end class"
            Dim expected = "
Imports System.Collections.Generic

class Test
    sub Method()
        dim t1 = New NewStruct(Goo(), Bar())
    end sub

    function goo() as object
    end function

    function bar() as object
    end function
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
        Return EqualityComparer(Of Object).Default.Equals(a, other.a) AndAlso
               EqualityComparer(Of Object).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertSingleTupleTypeWithInferredName(host As TestHost) As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||](a:=1, b)
    end sub
end class"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = New NewStruct(a:=1, b)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertMultipleInstancesInSameMethod(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertMultipleInstancesInSameMethod_DifferingCase(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertMultipleInstancesAcrossMethods(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function OnlyConvertMatchingTypesInSameMethod(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFixAllMatchesInSingleMethod(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFixNotAcrossMethods(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function NotIfReferencesAnonymousTypeInternally(host As TestHost) As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=new With { .c = 1, .d = 2 })
    end sub
end class"

            Await TestAsync(text, text, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod1(host As TestHost) As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=directcast((a:=1, b:=directcast(nothing, object)), object))
    end sub
end class"
            Dim expected = "
Imports System.Collections.Generic

class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, directcast(New NewStruct(a:=1, directcast(nothing, object)), object))
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
               EqualityComparer(Of Object).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod2(host As TestHost) As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=directcast([||](a:=1, b:=directcast(nothing, object)), object))
    end sub
end class"
            Dim expected = "
Imports System.Collections.Generic

class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, directcast(New NewStruct(a:=1, directcast(nothing, object)), object))
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
               EqualityComparer(Of Object).Default.Equals(b, other.b)
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function RenameAnnotationOnStartingPoint(host As TestHost) As Task
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function CapturedMethodTypeParameters(host As TestHost) As Task
            Dim text = "
imports System.Collections.Generic

class Test(of X as {structure})
    sub Method(of Y as {class, new})(x as List(of X), y1 as Y())
        dim t1 = [||](a:=x, b:=y1)
    end sub
end class"
            Dim expected = "
imports System.Collections.Generic

class Test(of X as {structure})
    sub Method(of Y as {class, new})(x as List(of X), y1 as Y())
        dim t1 = New NewStruct(Of X, Y)(x, y1)
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
        Return (a, b).GetHashCode()
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

            Await TestAsync(text, expected, testHost:=host, actions:={
                FeaturesResources.updating_usages_in_containing_member
            })
        End Function

        <Theory, CombinatorialData>
        Public Async Function NewTypeNameCollision(host As TestHost) As Task
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
        dim t1 = New NewStruct1(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function NewTypeNameCollision_CaseInsensitive(host As TestHost) As Task
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
        dim t1 = New NewStruct1(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInLambda1(host As TestHost) As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
        dim a = function ()
                    dim t2 = (a:=3, b:=4)
                end function()
    end sub
end class"
            Dim expected = "
imports System

class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
        dim a = function ()
                    dim t2 = New NewStruct(a:=3, b:=4)
                end function()
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInLambda2(host As TestHost) As Task
            Dim text = "
imports System

class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim a = function ()
                    dim t2 = [||](a:=3, b:=4)
                end function()
    end sub
end class"
            Dim expected = "
imports System

class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
        dim a = function ()
                    dim t2 = New NewStruct(a:=3, b:=4)
                end function()
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertWithDefaultNames1(host As TestHost) As Task
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
        dim t1 = New NewStruct(1, 2)
        dim t2 = New NewStruct(1, 2)
        dim t3 = (a:=1, b:=2)
        dim t4 = New NewStruct(item1:=1, item2:=2)
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
        Return (Item1, Item2).GetHashCode()
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

            Await TestAsync(text, expected, testHost:=host, actions:={
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            })
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertWithDefaultNames2(host As TestHost) As Task
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
        dim t4 = New NewStruct(item1:=1, item2:=2)
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
        Return (Item1, Item2).GetHashCode()
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
            Await TestAsync(text, expected, testHost:=host, actions:={
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            })
        End Function

        <Theory, CombinatorialData>
        Public Async Function ConvertSingleTupleTypeWithInaccessibleSystemHashCode(host As TestHost) As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||](a:=1, b:=2)
    end sub
end class"

            Dim hashCodeText = "
Namespace System
    Friend Class HashCode
    End Class
End Namespace"

            Dim expected = "
class Test
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
        Return (a, b).GetHashCode()
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

            Dim test = New VerifyVB.Test With {
                .TestHost = host
            }

            test.TestState.Sources.Add(text)
            test.TestState.AdditionalProjects("Assembly1").Sources.Add(hashCodeText)
            test.TestState.AdditionalProjectReferences.Add("Assembly1")

            test.FixedState.Sources.Add(expected)
            test.FixedState.AdditionalProjects("Assembly1").Sources.Add(hashCodeText)
            test.FixedState.AdditionalProjectReferences.Add("Assembly1")

            Await test.RunAsync()
        End Function

#End Region

#Region "update containing type tests"

        <Theory, CombinatorialData>
        Public Async Function TestCapturedTypeParameter_UpdateType(host As TestHost) As Task
            Dim text = "
imports System

class Test(of T)
    sub Method(t2 as T)
        dim t1 = [||](a:=t2, b:=2)
    end sub

    dim t3 as T
    sub Goo()
        dim t2 = (a:=t3, b:=4)
    end sub

    sub Blah(of T)(t1 as T)
        dim t2 = (a:=t1, b:=4)
    end sub
end class"
            Dim expected = "
imports System
Imports System.Collections.Generic

class Test(of T)
    sub Method(t2 as T)
        dim t1 = New NewStruct(Of T)(t2, b:=2)
    end sub

    dim t3 as T
    sub Goo()
        dim t2 = New NewStruct(Of T)(t3, b:=4)
    end sub

    sub Blah(of T)(t1 as T)
        dim t2 = (a:=t1, b:=4)
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
        Return EqualityComparer(Of T).Default.Equals(a, other.a) AndAlso
               b = other.b
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return (a, b).GetHashCode()
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

            Await TestAsync(text, expected, index:=1, equivalenceKey:=Scope.ContainingType.ToString(), testHost:=host, actions:={
                FeaturesResources.updating_usages_in_containing_member,
                FeaturesResources.updating_usages_in_containing_type
            })
        End Function

        <Theory, CombinatorialData>
        Public Async Function UpdateAllInType_SinglePart_SingleFile(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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
            Await TestAsync(text, expected, index:=1, equivalenceKey:=Scope.ContainingType.ToString(), testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function UpdateAllInType_MultiplePart_SingleFile(host As TestHost) As Task
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
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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

            Await TestAsync(text, expected, index:=1, equivalenceKey:=Scope.ContainingType.ToString(), testHost:=host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function UpdateAllInType_MultiplePart_MultipleFile(host As TestHost) As Task
            Dim text1 = "
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
end class"
            Dim text2 = "
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
end class"

            Dim expected1 = "
imports System

partial class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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

            Dim expected2 = "
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
end class"

            Dim test = New VerifyVB.Test With {
                .CodeActionIndex = 1,
                .CodeActionEquivalenceKey = Scope.ContainingType.ToString(),
                .TestHost = host
                }

            test.TestState.Sources.Add(text1)
            test.TestState.Sources.Add(text2)

            test.FixedState.Sources.Add(expected1)
            test.FixedState.Sources.Add(expected2)

            Await test.RunAsync()
        End Function

#End Region

#Region "update containing project tests"

        <Theory, CombinatorialData>
        Public Async Function UpdateAllInProject_MultiplePart_MultipleFile_WithNamespace(host As TestHost) As Task
            Dim text1 = "
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
end namespace"

            Dim text2 = "
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
end class"

            Dim expected1 = "
imports System

namespace N
    partial class Test
        sub Method()
            dim t1 = New NewStruct(a:=1, b:=2)
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
            Return (a, b).GetHashCode()
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
end namespace"

            Dim expected2 = "
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
end class"

            Dim test = New VerifyVB.Test with {
                .CodeActionIndex = 2,
                .CodeActionEquivalenceKey = Scope.ContainingProject.ToString(),
                .TestHost = host
                }

            test.TestState.Sources.Add(text1)
            test.TestState.Sources.Add(text2)

            test.FixedState.Sources.Add(expected1)
            test.FixedState.Sources.Add(expected2)

            Await test.RunAsync()
        End Function

#End Region

#Region "update dependent projects"

        <Theory, CombinatorialData>
        Public Async Function UpdateDependentProjects_DirectDependency(host As TestHost) As Task
            Dim text1 = "
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
end class"
            Dim text2 = "
imports System

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class"
            Dim expected1 = "
imports System

partial class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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

            Dim expected2 = "
imports System

partial class Other
    sub Goo()
        dim t1 = New NewStruct(a:=1, b:=2)
    end sub
end class"

            Dim test = New VerifyVB.Test With {
                .CodeActionIndex = 3,
                .CodeActionEquivalenceKey = Scope.DependentProjects.ToString(),
                .TestHost = host
            }

            test.TestState.Sources.Add(text1)
            test.TestState.AdditionalProjects("DependencyProject").Sources.Add(text2)
            test.TestState.AdditionalProjects("DependencyProject").AdditionalProjectReferences.Add("TestProject")

            test.FixedState.Sources.Add(expected1)
            test.FixedState.AdditionalProjects("DependencyProject").Sources.Add(expected2)
            test.FixedState.AdditionalProjects("DependencyProject").AdditionalProjectReferences.Add("TestProject")

            Await test.RunAsync()
        End Function

        <Theory, CombinatorialData>
        Public Async Function UpdateDependentProjects_NoDependency(host As TestHost) As Task
            Dim text1 = "
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
end class"
            Dim text2 = "
imports System

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class"
            Dim expected1 = "
imports System

partial class Test
    sub Method()
        dim t1 = New NewStruct(a:=1, b:=2)
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
        Return (a, b).GetHashCode()
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

            Dim expected2 = "
imports System

partial class Other
    sub Goo()
        dim t1 = (a:=1, b:=2)
    end sub
end class"

            Dim test = New VerifyVB.Test With {
                .CodeActionIndex = 3,
                .CodeActionEquivalenceKey = Scope.DependentProjects.ToString(),
                .TestHost = host
            }

            test.TestState.Sources.Add(text1)
            test.TestState.AdditionalProjects("DependencyProject").Sources.Add(text2)

            test.FixedState.Sources.Add(expected1)
            test.FixedState.AdditionalProjects("DependencyProject").Sources.Add(expected2)

            Await test.RunAsync()
        End Function

#End Region

    End Class
End Namespace
