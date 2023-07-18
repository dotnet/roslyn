' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class IReadOnlyListAndCollection
        Inherits BasicTestBase

        <Fact()>
        Public Sub IReadOnlyListTest()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic
Imports System

Module Module1
    Sub ProcessList(Of T)(list As IList(Of T))
        Console.WriteLine("ProcessList" + list.Count.ToString())
    End Sub

    Sub ProcessReadOnlyList(Of T)(list As IReadOnlyList(Of T))
        Console.WriteLine("ProcessReadOnlyList" + list.Count.ToString())
    End Sub

    Sub ProcessReadOnlyListOfObject(list As IReadOnlyList(Of Object))
        Console.WriteLine("ProcessReadOnlyListOfObject" + list.Count.ToString())
    End Sub

    Sub Main()
        Dim ints As Integer() = {1, 2, 3}
        Dim strings As String() = {"a", "b", "c"}

        Dim x As IReadOnlyList(Of Integer) = ints
        Console.WriteLine(x.Count)
        Dim rints As Integer() = x
        Console.WriteLine(rints.Length)

        Dim y As IReadOnlyList(Of String) = strings
        Console.WriteLine(y.Count)
        Dim rstrings As String() = y
        Console.WriteLine(rstrings.Length)

        Dim objs As Object() = {"1", "2", "3"}
        Try
            Dim y1 As IList(Of String) = objs
        Catch e As InvalidCastException
        End Try

        Dim objs1 As Object() = strings
        Dim y2 As IList(Of String) = CType(objs1, IList(Of String))
        Console.WriteLine(y2.Count)

        Dim intarrs As Integer()() = New Integer()() {New Integer() {1, 2, 3}}
        Dim a1 As IReadOnlyList(Of IReadOnlyList(Of Integer)) = intarrs
        Console.WriteLine(a1(0).Count)

        Dim a2 As IReadOnlyList(Of IList(Of Integer)) = intarrs
        Console.WriteLine(a2(0).Count)

        Dim a3 As IList(Of IReadOnlyList(Of Integer)) = intarrs
        Console.WriteLine(a3(0).Count)

        Dim a4 As IReadOnlyList(Of Integer()) = intarrs
        Console.WriteLine(a4(0).Length)

        Dim arrs As IReadOnlyList(Of Integer) = Nothing
        Dim a5 As IReadOnlyList(Of Integer()) = CType(arrs, IReadOnlyList(Of Integer()))

        ' Test type inference for the method type param.
        ProcessReadOnlyList(ints)
        ProcessReadOnlyList(Of Integer)(ints)

        ' Type inference reference types
        ProcessReadOnlyList(strings)
        ProcessReadOnlyList(Of String)(strings)

        ' Method group explicit cast.
        ProcessReadOnlyListOfObject(strings)

        ' Expression reclassification
        Dim c1 As IReadOnlyList(Of Short) = {1, 2, 3}

        Dim lambda = Function() As IReadOnlyList(Of String)

                         Return {}

                     End Function

        Dim conditional = If(True, x, ints)
    End Sub

    Function f() As IReadOnlyList(Of Short)

        Return {1, 2, 3}

    End Function

End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            ' ILVerify: Unexpected type on the stack. { Offset = 232, Found = ref 'int32[][]', Expected = ref '[mscorlib]System.Collections.Generic.IList`1<System.Collections.Generic.IReadOnlyList`1<int32>>' }
            CompileAndVerify(compilation,
            <![CDATA[
3
3
3
3
3
3
3
3
3
ProcessReadOnlyList3
ProcessReadOnlyList3
ProcessReadOnlyList3
ProcessReadOnlyList3
ProcessReadOnlyListOfObject3
]]>, verify:=Verification.FailsILVerify)

        End Sub

        <Fact()>
        Public Sub IReadOnlyCollectionTest()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic

Imports System
Imports System.Collections.ObjectModel
Imports System.Linq


Module Module1

    Sub ProcessList(Of T)(list As IList(Of T))

        Console.WriteLine("ProcessList" + list.Count.ToString())

    End Sub



    Sub ProcessReadOnlyCollection(Of T)(list As IReadOnlyCollection(Of T))

        Console.WriteLine("ProcessReadOnlyCollection" + list.Count.ToString())

    End Sub



    Sub ProcessReadOnlyCollectionOfObject(list As IReadOnlyCollection(Of Object))

        Console.WriteLine("ProcessReadOnlyCollectionOfObject" + list.Count.ToString())

    End Sub



    Sub Main()

        Dim ints As Integer() = {1, 2, 3}

        Dim strings As String() = {"a", "b", "c"}



        Dim x As IReadOnlyCollection(Of Integer) = ints

        Console.WriteLine(x.Count)

        Dim rints As Integer() = x

        Console.WriteLine(rints.Length)



        Dim y As IReadOnlyCollection(Of String) = strings

        Console.WriteLine(y.Count)

        Dim rstrings As String() = y

        Console.WriteLine(rstrings.Length)



        Dim objs As Object() = {"1", "2", "3"}

        Try

            Dim y1 As IList(Of String) = objs

        Catch e As InvalidCastException

        End Try



        Dim objs1 As Object() = strings

        Dim y2 As IList(Of String) = CType(objs1, IList(Of String))

        Console.WriteLine(y2.Count)



        Dim intarrs As Integer()() = New Integer()() {New Integer() {1, 2, 3}}

        Dim a1 As IReadOnlyCollection(Of IReadOnlyCollection(Of Integer)) = intarrs

        Console.WriteLine(a1(0).Count)



        Dim a2 As IReadOnlyCollection(Of IList(Of Integer)) = intarrs

        Console.WriteLine(a2(0).Count)



        Dim a3 As IList(Of IReadOnlyCollection(Of Integer)) = intarrs

        Console.WriteLine(a3(0).Count)



        Dim a4 As IReadOnlyCollection(Of Integer()) = intarrs

        Console.WriteLine(a4(0).Length)



        Dim arrs As IReadOnlyCollection(Of Integer) = Nothing

        Dim a5 As IReadOnlyCollection(Of Integer()) = CType(arrs, IReadOnlyCollection(Of Integer()))



        ' Test type inference for the method type param.

        ProcessReadOnlyCollection(ints)

        ProcessReadOnlyCollection(Of Integer)(ints)



        ' Type inference reference types

        ProcessReadOnlyCollection(strings)

        ProcessReadOnlyCollection(Of String)(strings)



        ' Method group explicit cast.

        ProcessReadOnlyCollectionOfObject(strings)



        ' Expression reclassification

        Dim c1 As IReadOnlyCollection(Of Short) = {1, 2, 3}



        Dim lambda = Function() As IReadOnlyCollection(Of String)


                         Return {}


                     End Function



        Dim conditional = If(True, x, ints)

        'Variance Test
        Dim base() As Base = {}
        Dim derived() As Derived = {}
        Dim ROCBase As IReadOnlyCollection(Of Base) = Nothing
        Dim ROCDerived As IReadOnlyCollection(Of Derived) = Nothing

        base = ROCDerived
        ROCBase = derived
        ROCDerived = base
        derived = ROCBase

        ROCBase = ROCDerived
        ROCDerived = ROCBase

    End Sub



    Function f() As IReadOnlyCollection(Of Short)
        Return {1, 2, 3}
    End Function

    Class Base : End Class
    Class Derived : Inherits Base : End Class

End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929}, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            ' ILVerify: Unexpected type on the stack. { Offset = 232, Found = ref 'int32[][]', Expected = ref '[mscorlib]System.Collections.Generic.IList`1<System.Collections.Generic.IReadOnlyCollection`1<int32>>' }
            CompileAndVerify(compilation,
            <![CDATA[
3
3
3
3
3
3
3
3
3
ProcessReadOnlyCollection3
ProcessReadOnlyCollection3
ProcessReadOnlyCollection3
ProcessReadOnlyCollection3
ProcessReadOnlyCollectionOfObject3
]]>, verify:=Verification.FailsILVerify)

        End Sub

    End Class
End Namespace
