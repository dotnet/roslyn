' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class IteratorTests
        Inherits FlowTestBase

        <Fact>
        Public Sub BasicIterator()
            Dim compilation = CreateCompilation(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System.Collections.Generic

Class C
    Public Iterator Function I As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
]]>
                        </file>
                    </compilation>).VerifyDiagnostics()

            Dim i = compilation.GetMember(Of MethodSymbol)("C.I")
            Assert.True(i.IsIterator)
            Assert.True(DirectCast(i, IMethodSymbol).IsIterator)
        End Sub

        <Fact>
        Public Sub BasicIterator_Metadata()
            Dim sourceComp = CreateCompilation(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System.Collections.Generic

Public Class C
    Public Iterator Function I As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class
]]>
                        </file>
                    </compilation>).VerifyDiagnostics()

            Dim userComp = CreateCompilation("", references:={sourceComp.EmitToImageReference()}).VerifyDiagnostics()
            Dim cMetadataType = Assert.IsAssignableFrom(Of PENamedTypeSymbol)(userComp.GetTypeByMetadataName("C"))

            Dim i = cMetadataType.GetMethod("I")
            Assert.False(i.IsIterator)
            Assert.False(DirectCast(i, IMethodSymbol).IsIterator)
        End Sub

        <Fact>
        Public Sub Method_NotIterator()
            Dim compilation = CreateCompilation(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System.Collections.Generic

Class C
    Public Function I As IEnumerable(Of Integer)
        Return System.Array.Empty(of Integer)()
    End Function
End Class
]]>
                        </file>
                    </compilation>).VerifyDiagnostics()

            Dim i = compilation.GetMember(Of MethodSymbol)("C.I")
            Assert.False(i.IsIterator)
            Assert.False(DirectCast(i, IMethodSymbol).IsIterator)
        End Sub

        <Fact()>
        Public Sub IteratorNoYields()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable
        'valid
    End Function

    Public Iterator Function goo1 As IEnumerable
        'valid
        Return
    End Function

    Public Iterator Function goo2 As IEnumerable
        'valid
        Exit Function
    End Function

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BadReturnValueInIterator()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable

        'valid
        Return

        'error
        Return 123

    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36942: To return a value from an Iterator function, use 'Yield' rather than 'Return'.
        Return 123
        ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BadYieldInNonIteratorMethod()

            ' Cannot get the actual BadYieldInNonIteratorMethod error since Yield keyword is conditional.
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable
        'valid
        Yield 123
    End Function

    Public Function goo1 As IEnumerable
        'error
        Yield 123
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30451: 'Yield' is not declared. It may be inaccessible due to its protection level.
        Yield 123
        ~~~~~
BC30800: Method arguments must be enclosed in parentheses.
        Yield 123
              ~~~
BC42105: Function 'goo1' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function
    ~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub YieldingUnassigned()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable
        Dim o as object
        Yield o
        
        o = 123
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC42104: Variable 'o' is used before it has been assigned a value. A null reference exception could result at runtime.
        Yield o
              ~
</errors>)
        End Sub

        <Fact>
        Public Sub YieldFlow()
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(
        <compilation>
            <file name="a.b"><![CDATA[
Class C

    Public Iterator Function goo() As IEnumerable

        Dim o As Object = 1

        Try
            [|Yield o|]

        Finally
            Console.WriteLine(o)

        End Try

    End Function

End Class

            ]]></file>
        </compilation>)
            Dim controlFlowAnalysisResults = analysisResults.Item1
            Dim dataFlowAnalysisResults = analysisResults.Item2
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count())
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count())
            Assert.Equal(True, controlFlowAnalysisResults.StartPointIsReachable)
            Assert.Equal(True, controlFlowAnalysisResults.EndPointIsReachable)
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside))
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside))
            Assert.Equal("Me, o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured))
        End Sub

        <Fact()>
        Public Sub YieldingFromTryCatchFinally()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable

        Try
            'valid
            Yield 1
        Catch ex As Exception
            'error
            Yield 1

        Finally
            'error
            Yield 1

        End Try
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC32042: Too few type arguments to 'IEnumerable(Of T)'.
    Public Iterator Function goo As IEnumerable
                                    ~~~~~~~~~~~
BC36939: 'Yield' cannot be used inside a 'Catch' statement or a 'Finally' statement.
            Yield 1
            ~~~~~~~
BC36939: 'Yield' cannot be used inside a 'Catch' statement or a 'Finally' statement.
            Yield 1
            ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub NoConversion()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable(of Exception)
        Yield 1       
    End Function

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30311: Value of type 'Integer' cannot be converted to 'Exception'.
        Yield 1       
              ~
</errors>)
        End Sub

        <Fact()>
        Public Sub NotReadable()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable(of Exception)
        Yield P1
    End Function

    WriteOnly Property P1 As Exception
        Set(value As Exception)

        End Set
    End Property

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30524: Property 'P1' is 'WriteOnly'.
        Yield P1
              ~~
</errors>)
        End Sub

        <Fact()>
        Public Sub LambdaConversions()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function goo As IEnumerable(of Func(of Integer, Integer))
        Yield Function() New Long
    End Function

    Public Iterator Function goo1 As IEnumerable(of Func(of String, Short, Integer))
        Yield Function(x, y) x.length + y
    End Function

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub InvalidParamType()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function f2(ByVal a As ArgIterator) As IEnumerator
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36932: 'ArgIterator' cannot be used as a parameter type for an Iterator or Async method.
    Public Iterator Function f2(ByVal a As ArgIterator) As IEnumerator
                                           ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub EnumeratorNotImported()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function f1(o As Object) As IEnumerable
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC32042: Too few type arguments to 'IEnumerable(Of T)'.
    Public Iterator Function f1(o As Object) As IEnumerable
                                                ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub ByRefParams()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
    End Sub

    Public Iterator Function f1(ByRef o As Object) As IEnumerable
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36927: Iterator methods cannot have ByRef parameters.
    Public Iterator Function f1(ByRef o As Object) As IEnumerable
                                ~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub IteratorTypeWrong()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
        Dim i = Iterator Function() As object
                    yield 123
                End Function
    End Sub

    Public Iterator Function f1( o As Object) As Object
        yield 123
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim i = Iterator Function() As object
                                       ~~~~~~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
    Public Iterator Function f1( o As Object) As Object
                                                 ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub SubIterator()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections

Module Module1
    Sub Main()
        Dim i = Iterator Sub()
                    yield 123
                End Sub

        Dim i1 = Iterator Sub() yield 123

    End Sub

    Public Iterator Sub f1( o As Object)
        yield 123
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim i = Iterator Sub()
                         ~~~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim i1 = Iterator Sub() yield 123
                          ~~~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
    Public Iterator Sub f1( o As Object)
                    ~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub StaticInIterator()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Iterator Function t4() As System.Collections.IEnumerator
        Static x As Integer = 1
    End Function
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36955: Static variables cannot appear inside Async or Iterator methods.
        Static x As Integer = 1
               ~
</errors>)
        End Sub

        <Fact()>
        Public Sub MiscInvalid()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
    End Sub

    <DllImport("user32.dll")> Iterator Function f3() As System.Collections.IEnumerator
    End Function

    ' Synchronized seems to be Ok
    <MethodImpl(MethodImplOptions.Synchronized)>
    Iterator Function f6() As System.Collections.IEnumerator
    End Function

    Declare iterator function f5 Lib "user32.dll" () as integer
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC37051: 'System.Runtime.InteropServices.DllImportAttribute' cannot be applied to an Async or Iterator method.
    &lt;DllImport("user32.dll")> Iterator Function f3() As System.Collections.IEnumerator
                                                ~~
BC30215: 'Sub' or 'Function' expected.
    Declare iterator function f5 Lib "user32.dll" () as integer
            ~
BC30218: 'Lib' expected.
    Declare iterator function f5 Lib "user32.dll" () as integer
                     ~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub IteratorInWrongPlaces()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim x = {Iterator sub() yield, new object}
        Dim y = {Iterator sub() yield 1, Iterator sub() yield, new object}
        Dim z = {Sub() AddHandler, New Object}
        g0(Iterator sub() Yield)
        g1(Iterator Sub() Yield, 5)
    End Sub

    Sub g0(ByVal x As Func(Of IEnumerator))
    End Sub
    Sub g1(ByVal x As Func(Of IEnumerator), ByVal y As Integer)
    End Sub

    Iterator Function f() As IEnumerator
        Yield
    End Function

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim x = {Iterator sub() yield, new object}
                          ~~~
BC30201: Expression expected.
        Dim x = {Iterator sub() yield, new object}
                                     ~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim y = {Iterator sub() yield 1, Iterator sub() yield, new object}
                          ~~~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim y = {Iterator sub() yield 1, Iterator sub() yield, new object}
                                                  ~~~
BC30201: Expression expected.
        Dim y = {Iterator sub() yield 1, Iterator sub() yield, new object}
                                                             ~
BC30201: Expression expected.
        Dim z = {Sub() AddHandler, New Object}
                                 ~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        g0(Iterator sub() Yield)
                    ~~~
BC30201: Expression expected.
        g0(Iterator sub() Yield)
                               ~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        g1(Iterator Sub() Yield, 5)
                    ~~~
BC30201: Expression expected.
        g1(Iterator Sub() Yield, 5)
                               ~
BC32042: Too few type arguments to 'IEnumerator(Of T)'.
    Sub g0(ByVal x As Func(Of IEnumerator))
                              ~~~~~~~~~~~
BC32042: Too few type arguments to 'IEnumerator(Of T)'.
    Sub g1(ByVal x As Func(Of IEnumerator), ByVal y As Integer)
                              ~~~~~~~~~~~
BC32042: Too few type arguments to 'IEnumerator(Of T)'.
    Iterator Function f() As IEnumerator
                             ~~~~~~~~~~~
BC30201: Expression expected.
        Yield
             ~
</errors>)
        End Sub

        <Fact()>
        Public Sub InferenceByrefLike()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim i = Iterator Function()
                    yield New ArgIterator

                End Function
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
    BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim i = Iterator Function()
                ~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub InferenceByref()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
    End Sub

    Public sub rr(byref x as Integer)
        Dim i = Iterator Function()
                    Yield x
                End Function
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36639: 'ByRef' parameter 'x' cannot be used in a lambda expression.
                    Yield x
                          ~
</errors>)
        End Sub

        <Fact()>
        Public Sub InferenceErrors_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())

        ' not an error
        Dim o As Object = Iterator Function()
                                       Yield 1
                                   End Function

        Dim i As Func(Of String) = Iterator Function()
                                       Yield 1
                                   End Function

        goo(i)

        bar(Iterator Function()
                Yield 1
            End Function)
    End Sub

    Public Sub goo(Of T)(x As Func(Of T))
        Console.WriteLine(GetType(T))
        Console.WriteLine(x.GetType())
    End Sub

    Public Sub bar(x As Func(Of String))
        Console.WriteLine(x.GetType())
    End Sub

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Dim i As Func(Of String) = Iterator Function()
                                   ~~~~~~~~~~~~~~~~~~~
BC36938: Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        bar(Iterator Function()
            ~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub InferenceErrors_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        baz(Iterator Function()
                Yield 1
            End Function)
    End Sub

    Public Sub baz(Of T)(x As Func(Of IEnumerator(Of T)))
        Console.WriteLine(GetType(T))
        Console.WriteLine(x.GetType())
    End Sub

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub baz(Of T)(x As Func(Of IEnumerator(Of T)))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        baz(Iterator Function()
        ~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub InferenceErrors_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        baz(Iterator Function()
                Yield 1
            End Function)
    End Sub

    Public Sub baz(Of T)(x As Func(Of IEnumerable(Of T)))
        Console.WriteLine(GetType(T))
        Console.WriteLine(x.GetType())
    End Sub

End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact(), WorkItem(629565, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629565")>
        Public Sub Bug629565()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Option Strict Off

Imports System
Imports System.Collections
Imports System.Collections.Generic

Module ModuleChanges2
    Sub Goo(x1 As Func(Of IEnumerable(Of Object)))
        System.Console.WriteLine("Object")
    End Sub
    Sub Goo(x2 As Func(Of IEnumerable(Of Integer)))
        System.Console.WriteLine("Integer")
    End Sub
    Sub Bar(x1 As Func(Of IEnumerable(Of Object)))
        System.Console.WriteLine("Object")
    End Sub
End Module

Module Program

    Sub Main()
        Goo(Iterator Function()
                Yield 2.0
            End Function)
        Bar(Iterator Function()
                Yield 2.0
            End Function)
    End Sub
End Module
]]>
                        </file>
                    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
            <![CDATA[
Object
Object
]]>)
        End Sub

        <Fact(), WorkItem(1006315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1006315")>
        Public Sub BadAsyncSingleLineLambda()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Module Program
    Function E() As IEnumerable(Of Integer)
        Return Nothing
    End Function
    Sub Main(args As String())
        Dim x As Func(Of IEnumerable(Of Integer)) = Iterator Function() E()
    End Sub
End Module
]]>
                        </file>
                    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36947: Single-line lambdas cannot have the 'Iterator' modifier. Use a multiline lambda instead.
        Dim x As Func(Of IEnumerable(Of Integer)) = Iterator Function() E()
                                                    ~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact(), WorkItem(1173145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1173145"), WorkItem(2862, "https://github.com/dotnet/roslyn/issues/2862")>
        Public Sub CompoundAssignmentToAField()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Imports System.Collections 
Imports System.Collections.Generic 

Module Module1
    Sub Main()
        For Each x In New MyEnumerable(Of Integer)({100, 99, 98})
            System.Console.WriteLine(x)
        Next
    End Sub
End Module

Public Class MyEnumerable(Of T)
    Implements IEnumerable(Of T)

    Private ReadOnly _items As T()
    Private _count As Integer = 0

    Public Sub New(items As T())
        _items = items
        _count = items.Length
    End Sub

    Public Iterator Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        For Each item In _items
            If _count = 0 Then Exit Function
            _count -= 1
            Yield item
        Next
    End Function

    Public Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function
End Class
]]>
                        </file>
                    </compilation>, TestOptions.DebugExe)

            Dim expected As Xml.Linq.XCData = <![CDATA[
100
99
98
]]>

            CompileAndVerify(compilation, expected)

            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expected)
        End Sub

        <Fact>
        Public Sub SimpleIteratorProperty()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic

Class C
    Iterator ReadOnly Property P As IEnumerable(Of Integer)
        Get
            Yield 1
        End Get
    End Property
End Class
]]></file>
</compilation>).VerifyDiagnostics()

            Dim [property] = compilation.GetMember(Of PropertySymbol)("C.P")
            Assert.True([property].GetMethod.IsIterator)
            Assert.True(DirectCast([property].GetMethod, IMethodSymbol).IsIterator)
        End Sub

        <Fact>
        <WorkItem(11531, "https://github.com/dotnet/roslyn/issues/11531")>
        <WorkItem(220696, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=220696")>
        Public Sub WritableIteratorProperty()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
MustInherit Class A
    MustOverride Iterator Property P As IEnumerable(Of Object)
End Class
Class B
    Private _p As IEnumerable(Of Object)
    Iterator Property P As IEnumerable(Of Object)
        Get
            For Each o in _p
                Yield o
            Next
        End Get
        Set
            _p = value
        End Set
    End Property
    Shared Sub Main()
        Dim b As New B()
        b.P = {1, 2, 3}
        For Each o in b.P
            Console.Write(o)
        Next
    End Sub
End Class
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugExe) ' generate debug info
            compilation.AssertTheseEmitDiagnostics(<expected/>)
            Dim [property] = compilation.GetMember(Of PropertySymbol)("A.P")
            Assert.True([property].GetMethod.IsIterator)
            Assert.True(DirectCast([property].GetMethod, IMethodSymbol).IsIterator)
            Assert.False([property].SetMethod.IsIterator)
            Assert.False(DirectCast([property].SetMethod, IMethodSymbol).IsIterator)
            [property] = compilation.GetMember(Of PropertySymbol)("B.P")
            Assert.True([property].GetMethod.IsIterator)
            Assert.True(DirectCast([property].GetMethod, IMethodSymbol).IsIterator)
            Assert.False([property].SetMethod.IsIterator)
            Assert.False(DirectCast([property].SetMethod, IMethodSymbol).IsIterator)
            CompileAndVerify(compilation, expectedOutput:="123")
        End Sub

        <Fact>
        Public Sub IteratorProperty_Metadata()
            Dim sourceComp = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic

Class C
    Iterator ReadOnly Property P As IEnumerable(Of Integer)
        Get
            Yield 1
        End Get
    End Property
End Class
]]></file>
</compilation>).VerifyDiagnostics()

            Dim userComp = CreateCompilation("", references:={sourceComp.EmitToImageReference()}).VerifyDiagnostics()
            Dim cMetadataType = Assert.IsAssignableFrom(Of PENamedTypeSymbol)(userComp.GetTypeByMetadataName("C"))

            Dim [property] = cMetadataType.GetProperty("P")
            Assert.False([property].GetMethod.IsIterator)
            Assert.False(DirectCast([property].GetMethod, IMethodSymbol).IsIterator)
        End Sub

        <Fact>
        Public Sub Property_NotIterator()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic

Class C
    ReadOnly Property P As IEnumerable(Of Integer)
        Get
            Return System.Array.Empty(Of Integer)()
        End Get
    End Property
End Class
]]></file>
</compilation>).VerifyDiagnostics()

            Dim [property] = compilation.GetMember(Of PropertySymbol)("C.P")
            Assert.False([property].GetMethod.IsIterator)
            Assert.False(DirectCast([property].GetMethod, IMethodSymbol).IsIterator)
        End Sub

        <Fact()>
        Public Sub CompilerLoweringPreserveAttribute_01()
            Dim source1 = "
Imports System
Imports System.Runtime.CompilerServices

<CompilerLoweringPreserve>
<AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter)>
Public Class Preserve1Attribute
    Inherits Attribute
End Class

<CompilerLoweringPreserve>
<AttributeUsage(AttributeTargets.Parameter)>
Public Class Preserve2Attribute
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter)>
Public Class Preserve3Attribute
    Inherits Attribute
End Class
"
            Dim source2 = "
Imports System.Collections.Generic

Class Test1
    Iterator Function M2(<Preserve1,Preserve2,Preserve3> x As Integer) As IEnumerable(Of Integer)
        Yield x
    End Function
End Class
"

            Dim validate = Sub(m As ModuleSymbol)
                               AssertEx.SequenceEqual(
                                   {"Preserve1Attribute"},
                                   m.GlobalNamespace.GetMember("Test1.VB$StateMachine_1_M2.$VB$Local_x").GetAttributes().Select(Function(a) a.ToString()))

                               AssertEx.SequenceEqual(
                                   {"Preserve1Attribute"},
                                   m.GlobalNamespace.GetMember("Test1.VB$StateMachine_1_M2.$P_x").GetAttributes().Select(Function(a) a.ToString()))
                           End Sub

            Dim comp1 = CreateCompilation(
                {source1, source2, CompilerLoweringPreserveAttributeDefinition},
                options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            CompileAndVerify(comp1, symbolValidator:=validate).VerifyDiagnostics()
        End Sub

    End Class

End Namespace
