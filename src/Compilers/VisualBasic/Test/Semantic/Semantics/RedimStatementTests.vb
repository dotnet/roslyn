' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class RedimStatementTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestRedimWithSimpleArray()
            Dim source =
<compilation name="TestRedimWithSimpleArray">
    <file name="a.vb">
Option Strict On
Imports System
Class RedimTest1
    Public Sub Main()
        Dim b As Object(,)
        ReDim Preserve b(1, 2)
        ReDim b(2, 1)

        ReDim b(a:=1, 2)
        ReDim Preserve b(a:=1, b:=2)

        ReDim b
        ReDim Preserve b()
        ReDim b(1)
        ReDim Preserve b(, 2)
        ReDim b(1, 2, 3)

        ReDim b(1D, "")
    End Sub
End Class    
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30075: Named arguments are not valid as array subscripts.
        ReDim b(a:=1, 2)
                ~~~~
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        ReDim b(a:=1, 2)
                      ~
BC30075: Named arguments are not valid as array subscripts.
        ReDim Preserve b(a:=1, b:=2)
                         ~~~~
BC30075: Named arguments are not valid as array subscripts.
        ReDim Preserve b(a:=1, b:=2)
                               ~~~~
BC30670: 'ReDim' statements require a parenthesized list of the new bounds of each dimension of the array.
        ReDim b
              ~~
BC30670: 'ReDim' statements require a parenthesized list of the new bounds of each dimension of the array.
        ReDim Preserve b()
                       ~~~
BC30415: 'ReDim' cannot change the number of dimensions of an array.
        ReDim b(1)
              ~~~~
BC30306: Array subscript expression missing.
        ReDim Preserve b(, 2)
                         ~
BC30415: 'ReDim' cannot change the number of dimensions of an array.
        ReDim b(1, 2, 3)
              ~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Decimal' to 'Integer'.
        ReDim b(1D, "")
                ~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
        ReDim b(1D, "")
                    ~~
</errors>)
        End Sub

        <Fact>
        Public Sub TestRedimWithProperty()
            Dim source =
<compilation name="TestRedimWithProperty">
    <file name="a.vb">
Imports System
Class RedimTest2
    Public Sub Main()
        ReDim ReadWriteProperty(1, 2)
        ReDim Preserve ReadWriteProperty()(1, 2)

        ReDim WriteOnlyProperty(1, 2)
        ReDim Preserve WriteOnlyProperty()(1, 2)

        ReDim ReadOnlyProperty(1, 2)
        ReDim Preserve ReadOnlyProperty()(1, 2)
    End Sub

    Public Property ReadWriteProperty As Integer(,)
        Get
            Return Nothing
        End Get
        Set(value As Integer(,))
        End Set
    End Property

    Public WriteOnly Property WriteOnlyProperty As Integer(,)
        Set(value As Integer(,))
        End Set
    End Property

    Public ReadOnly Property ReadOnlyProperty As Integer(,)
        Get
            Return Nothing
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30524: Property 'WriteOnlyProperty' is 'WriteOnly'.
        ReDim Preserve WriteOnlyProperty()(1, 2)
                       ~~~~~~~~~~~~~~~~~~~
BC30526: Property 'ReadOnlyProperty' is 'ReadOnly'.
        ReDim ReadOnlyProperty(1, 2)
              ~~~~~~~~~~~~~~~~
BC30526: Property 'ReadOnlyProperty' is 'ReadOnly'.
        ReDim Preserve ReadOnlyProperty()(1, 2)
                       ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub TestRedimWithOtherExpressions()
            Dim source =
<compilation name="TestRedimWithOtherExpressions">
    <file name="a.vb">
Imports System
Class RedimTest3
    Public Sub Main()
        ReDim F()(1, 2)
        ReDim Preserve F(1, 2), F(1), F(), F

        ReDim 1(1, 2)
        ReDim (1 + 1)(1, 2), F(1,2)(1, 2)
    End Sub

    Public Function F() As Integer(,)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim F()(1, 2)
              ~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim Preserve F(1, 2), F(1), F(), F
                       ~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim Preserve F(1, 2), F(1), F(), F
                                ~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim Preserve F(1, 2), F(1), F(), F
                                      ~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        ReDim Preserve F(1, 2), F(1), F(), F
                                           ~
BC30074: Constant cannot be the target of an assignment.
        ReDim 1(1, 2)
              ~
BC30074: Constant cannot be the target of an assignment.
        ReDim (1 + 1)(1, 2), F(1,2)(1, 2)
              ~~~~~~~
BC30049: 'Redim' statement requires an array.
        ReDim (1 + 1)(1, 2), F(1,2)(1, 2)
                             ~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub TestRedimWithObject()
            Dim source =
<compilation name="TestRedimWithObject">
    <file name="a.vb">
Class RedimTest4
    Public Sub Main()
        Dim o As New Object

        ReDim o
        ReDim Preserve o()
        ReDim o(1, 2)
        ReDim Preserve o(1, 2, 3)

        ReDim ReadWriteProperty(1, 2)
        ReDim Preserve ReadWriteProperty(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                                         11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                                         21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                                         31, 32)
        ReDim ReadWriteProperty(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                                11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                                21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                                31, 32, 33)
    End Sub

    Public Property ReadWriteProperty As Object
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30670: 'ReDim' statements require a parenthesized list of the new bounds of each dimension of the array.
        ReDim o
              ~~
BC30670: 'ReDim' statements require a parenthesized list of the new bounds of each dimension of the array.
        ReDim Preserve o()
                       ~~~
BC30052: Array exceeds the limit of 32 dimensions.
        ReDim ReadWriteProperty(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~    
</errors>)
        End Sub

        <WorkItem(541971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541971")>
        <Fact>
        Public Sub TestIndexSpecifiedWithRange()
            Dim source =
<compilation name="TestIndexSpecifiedWithRange">
    <file name="a.vb">
Module M
    Sub Main()
        Dim x()() As Object = New Object(2)() {}
        ReDim x(0 To 1)(0 To 1)
    End Sub
End Module
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(541971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541971")>
        <Fact>
        Public Sub TestIndexSpecifiedWithRange2()
            Dim source =
<compilation name="TestIndexSpecifiedWithRange2">
    <file name="a.vb">
Module M
    Sub Main()
        Dim x()() As Object = New Object(2)() {}
        ReDim x(0.0 To 1)(0 To 1)
    End Sub
End Module
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC32059: Array lower bounds can be only '0'.
        ReDim x(0.0 To 1)(0 To 1)
                ~~~
</errors>)
        End Sub

        <WorkItem(541971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541971")>
        <Fact>
        Public Sub TestIndexSpecifiedWithRange3()
            Dim source =
<compilation name="TestIndexSpecifiedWithRange3">
    <file name="a.vb">
Module M
    Sub Main()
        Dim x()() As Object = New Object(2)() {}
        ReDim x(0 To 1)(0.0 To 1)
    End Sub
End Module
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC32059: Array lower bounds can be only '0'.
        ReDim x(0 To 1)(0.0 To 1)
                        ~~~
</errors>)
        End Sub

        <Fact>
        Public Sub TestNoCopyArray()
            Dim source =
<compilation name="TestNoCopyArray">
    <file name="a.vb">
Class RedimTest4
    Public Sub Main()
        Dim o As New Object
        ReDim Preserve o(1, 2)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Utils.CopyArray' is not defined.
        ReDim Preserve o(1, 2)
                       ~~~~~~~
</errors>)
        End Sub

    End Class
End Namespace

