' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class Lambda_AnonymousDelegateInference
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
    Dim x = Function(y) y
    System.Console.WriteLine(x.GetType())

    x = Function(y) y ' No inference here, using delegate type from x.
    System.Console.WriteLine(x.GetType())

    Dim x2 As Object = Function(y) y
    System.Console.WriteLine(x2.GetType())

    Dim x3 = Function() Function() "a"
    System.Console.WriteLine(x3.GetType())

    Dim x4 = Function()
                    Return Function() 2.5
                End Function
    System.Console.WriteLine(x4.GetType())

    Dim x5 = DirectCast(Function() Date.Now, System.Delegate)
    System.Console.WriteLine(x5.GetType())

    Dim x6 = TryCast(Function() Decimal.MaxValue, System.MulticastDelegate)
    System.Console.WriteLine(x6.GetType())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
VB$AnonymousDelegate_0`2[System.Object,System.Object]
VB$AnonymousDelegate_0`2[System.Object,System.Object]
VB$AnonymousDelegate_0`2[System.Object,System.Object]
VB$AnonymousDelegate_1`1[VB$AnonymousDelegate_1`1[System.String]]
VB$AnonymousDelegate_1`1[VB$AnonymousDelegate_1`1[System.Double]]
VB$AnonymousDelegate_1`1[System.DateTime]
VB$AnonymousDelegate_1`1[System.Decimal]
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
VB$AnonymousDelegate_0`2[System.Object,System.Object]
VB$AnonymousDelegate_0`2[System.Object,System.Object]
VB$AnonymousDelegate_0`2[System.Object,System.Object]
VB$AnonymousDelegate_1`1[VB$AnonymousDelegate_1`1[System.String]]
VB$AnonymousDelegate_1`1[VB$AnonymousDelegate_1`1[System.Double]]
VB$AnonymousDelegate_1`1[System.DateTime]
VB$AnonymousDelegate_1`1[System.Decimal]
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
    Dim x = Function(y) y
                     ~
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
    Dim x2 As Object = Function(y) y
                                ~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
    Dim x = Function(y) y
                     ~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
    Dim x2 As Object = Function(y) y
                                ~
</expected>)
        End Sub

        <Fact>
        Public Sub Test2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x1 = Function() Unknown 'BIND1:"x1"

        Dim x2 = Function() : 'BIND2:"x2"

        Dim x3 = Function() 'BIND3:"x3"
                     Return Unknown '3
                 End Function

        Dim x4 = Function() 'BIND4:"x4"
                     Unknown()
                 End Function '4

        Dim x5 = Function() AddressOf Main 'BIND5:"x5"

        Dim x6 = Function() 'BIND6:"x6"
                     Return AddressOf Main '6 
                 End Function

    End Sub

    Delegate Sub D1(x As System.ArgIterator)

    Sub Test(y As System.ArgIterator)

        Dim x7 = Function() y 'BIND7:"x7"

        Dim x8 = Function() 'BIND8:"x8"
                     Return y '8
                 End Function

        Dim x9 = Sub(x As System.ArgIterator) System.Console.WriteLine()

        ' The following Dim shouldn't produce any errors.
        Dim x10 As D1 = Sub(x As System.ArgIterator) System.Console.WriteLine()

        Dim x11 = Sub(x() As System.ArgIterator) System.Console.WriteLine()
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each strict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(strict))

                CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
        <![CDATA[
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
        Dim x1 = Function() Unknown 'BIND1:"x1"
                            ~~~~~~~
BC30201: Expression expected.
        Dim x2 = Function() : 'BIND2:"x2"
                            ~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
                     Return Unknown '3
                            ~~~~~~~
BC30451: 'Unknown' is not declared. It may be inaccessible due to its protection level.
                     Unknown()
                     ~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                 End Function '4
                 ~~~~~~~~~~~~
BC30581: 'AddressOf' expression cannot be converted to 'Object' because 'Object' is not a delegate type.
        Dim x5 = Function() AddressOf Main 'BIND5:"x5"
                            ~~~~~~~~~~~~~~
BC36751: Cannot infer a return type.  Consider adding an 'As' clause to specify the return type.
        Dim x6 = Function() 'BIND6:"x6"
                 ~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x7 = Function() y 'BIND7:"x7"
                 ~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x8 = Function() 'BIND8:"x8"
                 ~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x9 = Sub(x As System.ArgIterator) System.Console.WriteLine()
                          ~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x11 = Sub(x() As System.ArgIterator) System.Console.WriteLine()
                             ~~~~~~~~~~~~~~~~~~
]]>
    </expected>)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                If True Then
                    Dim node1 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
                    Dim x1 = DirectCast(semanticModel.GetDeclaredSymbol(node1), LocalSymbol)
                    Assert.Equal("x1", x1.Name)
                    Assert.Equal("Function <generated method>() As ?", x1.Type.ToTestDisplayString)
                    Assert.True(x1.Type.IsAnonymousType)
                    Assert.Same(LambdaSymbol.ReturnTypeIsUnknown, DirectCast(x1.Type, NamedTypeSymbol).DelegateInvokeMethod.ReturnType)
                End If

                If True Then
                    Dim node2 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
                    Dim x2 = DirectCast(semanticModel.GetDeclaredSymbol(node2), LocalSymbol)
                    Assert.Equal("x2", x2.Name)
                    Assert.Equal("Function <generated method>() As ?", x2.Type.ToTestDisplayString)
                    Assert.True(x2.Type.IsAnonymousType)
                    Assert.Same(LambdaSymbol.ReturnTypeIsUnknown, DirectCast(x2.Type, NamedTypeSymbol).DelegateInvokeMethod.ReturnType)
                End If

                If True Then
                    Dim node3 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 3)
                    Dim x3 = DirectCast(semanticModel.GetDeclaredSymbol(node3), LocalSymbol)
                    Assert.Equal("x3", x3.Name)
                    Assert.Equal("Function <generated method>() As ?", x3.Type.ToTestDisplayString)
                    Assert.True(x3.Type.IsAnonymousType)
                    Assert.Same(LambdaSymbol.ReturnTypeIsUnknown, DirectCast(x3.Type, NamedTypeSymbol).DelegateInvokeMethod.ReturnType)
                End If

                If True Then
                    Dim node4 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
                    Dim x4 = DirectCast(semanticModel.GetDeclaredSymbol(node4), LocalSymbol)
                    Assert.Equal("x4", x4.Name)
                    Assert.Equal("Function <generated method>() As ?", x4.Type.ToTestDisplayString)
                    Assert.True(x4.Type.IsAnonymousType)
                    Assert.Same(LambdaSymbol.ReturnTypeIsUnknown, DirectCast(x4.Type, NamedTypeSymbol).DelegateInvokeMethod.ReturnType)
                End If

                If True Then
                    Dim node5 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 5)
                    Dim x5 = DirectCast(semanticModel.GetDeclaredSymbol(node5), LocalSymbol)
                    Assert.Equal("x5", x5.Name)
                    Assert.Equal("Function <generated method>() As System.Object", x5.Type.ToTestDisplayString)
                    Assert.True(x5.Type.IsAnonymousType)
                    Assert.True(DirectCast(x5.Type, NamedTypeSymbol).DelegateInvokeMethod.ReturnType.IsObjectType())
                End If

                If True Then
                    Dim node6 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 6)
                    Dim x6 = DirectCast(semanticModel.GetDeclaredSymbol(node6), LocalSymbol)
                    Assert.Equal("x6", x6.Name)
                    Assert.Equal("Function <generated method>() As ?", x6.Type.ToTestDisplayString)
                    Assert.True(x6.Type.IsAnonymousType)
                    Assert.Same(LambdaSymbol.ReturnTypeIsUnknown, DirectCast(x6.Type, NamedTypeSymbol).DelegateInvokeMethod.ReturnType)
                End If

                If True Then
                    Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
                    Dim x7 = DirectCast(semanticModel.GetDeclaredSymbol(node7), LocalSymbol)
                    Assert.Equal("x7", x7.Name)
                    Assert.Equal("Function <generated method>() As System.ArgIterator", x7.Type.ToTestDisplayString)
                    Assert.True(x7.Type.IsAnonymousType)
                End If

                If True Then
                    Dim node8 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 8)
                    Dim x8 = DirectCast(semanticModel.GetDeclaredSymbol(node8), LocalSymbol)
                    Assert.Equal("x8", x8.Name)
                    Assert.Equal("Function <generated method>() As System.ArgIterator", x8.Type.ToTestDisplayString)
                    Assert.True(x8.Type.IsAnonymousType)
                End If
            Next

        End Sub

        <Fact>
        Public Sub Test3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Interface I1
End Interface

Interface I2
End Interface

Interface I3
    Inherits I2, I1
End Interface

Interface I4
    Inherits I2, I1
End Interface

Module Program
  Sub Main()
        Dim x1 = Function(y As Integer)
                     If y > 0 Then
                         Return New System.Collections.Generic.Dictionary(Of Integer, Integer)
                     Else
                         Return New System.Collections.Generic.Dictionary(Of Byte, Byte)
                     End If
                 End Function

        System.Console.WriteLine(x1.GetType())

        Dim x2 = Function(y1 As I3, y2 As I4)
                     If True Then Return y1 Else Return y2
                 End Function

        System.Console.WriteLine(x2.GetType())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
VB$AnonymousDelegate_0`2[System.Int32,System.Object]
VB$AnonymousDelegate_1`3[I3,I4,System.Object]
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
VB$AnonymousDelegate_0`2[System.Int32,System.Object]
VB$AnonymousDelegate_1`3[I3,I4,System.Object]
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42021: Cannot infer a return type; 'Object' assumed.
        Dim x1 = Function(y As Integer)
                 ~~~~~~~~~~~~~~~~~~~~~~
BC42021: Cannot infer a return type because more than one type is possible; 'Object' assumed.
        Dim x2 = Function(y1 As I3, y2 As I4)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36916: Cannot infer a return type. Consider adding an 'As' clause to specify the return type.
        Dim x1 = Function(y As Integer)
                 ~~~~~~~~~~~~~~~~~~~~~~
BC36734: Cannot infer a return type because more than one type is possible. Consider adding an 'As' clause to specify the return type.
        Dim x2 = Function(y1 As I3, y2 As I4)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Test4()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Option Strict ON

Module Program
  Sub Main()
  End Sub

    Sub Test(y As System.ArgIterator)

        Dim x7 = Function(x) y

        Dim x8 = Function(x)
                     Return y '8
                 End Function

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x7 = Function(x) y
                 ~~~~~~~~~~~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        Dim x7 = Function(x) y
                          ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x8 = Function(x)
                 ~~~~~~~~~~~
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        Dim x8 = Function(x)
                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub Test5()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Call (Sub(x As Integer) System.Console.WriteLine(x))(1) 'BIND1:"Sub(x As Integer)"

        Call Sub(x As Integer) 'BIND2:"Sub(x As Integer)"
                 System.Console.WriteLine(x)
             End Sub(2)

        Dim x3 As Integer = Function() As Integer 'BIND3:"Function() As Integer"
                                Return 3
                            End Function.Invoke()
        System.Console.WriteLine(x3)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            If True Then
                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("Sub <generated method>(x As System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Widening, Lambda", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If
            If True Then
                Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("Sub <generated method>(x As System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node2.Parent)
                Assert.Equal("Widening, Lambda", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If
            If True Then
                Dim node3 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 3)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node3.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("Function <generated method>() As System.Int32", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node3.Parent)
                Assert.Equal("Widening, Lambda", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine & "3")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      131 (0x83)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Integer)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldc.i4.1
  IL_0025:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_002a:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_002f:  brfalse.s  IL_0038
  IL_0031:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0036:  br.s       IL_004e
  IL_0038:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_003d:  ldftn      "Sub Program._Closure$__._Lambda$__0-1(Integer)"
  IL_0043:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0048:  dup
  IL_0049:  stsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_004e:  ldc.i4.2
  IL_004f:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_0054:  ldsfld     "Program._Closure$__.$I0-2 As <generated method>"
  IL_0059:  brfalse.s  IL_0062
  IL_005b:  ldsfld     "Program._Closure$__.$I0-2 As <generated method>"
  IL_0060:  br.s       IL_0078
  IL_0062:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0067:  ldftn      "Function Program._Closure$__._Lambda$__0-2() As Integer"
  IL_006d:  newobj     "Sub VB$AnonymousDelegate_1(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0072:  dup
  IL_0073:  stsfld     "Program._Closure$__.$I0-2 As <generated method>"
  IL_0078:  callvirt   "Function VB$AnonymousDelegate_1(Of Integer).Invoke() As Integer"
  IL_007d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0082:  ret
}
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

        End Sub

        <Fact()>
        Public Sub Test6()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim y1 as Integer = 2
        Dim y2 as Integer = 3
        Dim y3 as Integer = 4
        Call (Sub(x As Integer) System.Console.WriteLine(x+y1))(1)

        Call Sub(x As Integer)
                 System.Console.WriteLine(x+y2)
             End Sub(2)

        Dim x3 As Integer = Function() As Integer
                                Return 3+y3
                            End Function.Invoke()
        System.Console.WriteLine(x3)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="3" & Environment.NewLine & "5" & Environment.NewLine & "7")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  3
  IL_0000:  newobj     "Sub Program._Closure$__0-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.2
  IL_0007:  stfld      "Program._Closure$__0-0.$VB$Local_y1 As Integer"
  IL_000c:  dup
  IL_000d:  ldc.i4.3
  IL_000e:  stfld      "Program._Closure$__0-0.$VB$Local_y2 As Integer"
  IL_0013:  dup
  IL_0014:  ldc.i4.4
  IL_0015:  stfld      "Program._Closure$__0-0.$VB$Local_y3 As Integer"
  IL_001a:  dup
  IL_001b:  ldc.i4.1
  IL_001c:  callvirt   "Sub Program._Closure$__0-0._Lambda$__0(Integer)"
  IL_0021:  dup
  IL_0022:  ldc.i4.2
  IL_0023:  callvirt   "Sub Program._Closure$__0-0._Lambda$__1(Integer)"
  IL_0028:  callvirt   "Function Program._Closure$__0-0._Lambda$__2() As Integer"
  IL_002d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0032:  ret
}
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

        End Sub

        <WorkItem(543286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543286")>
        <Fact()>
        Public Sub AnonDelegateReturningLambdaWithGenericType()
            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System

Module S1
    Public Function Goo(Of T)() As System.Func(Of System.Func(Of T))
        Dim x2 = Function()
                     Return Function() As T
                                Return Nothing
                            End Function
                 End Function

        Return x2
    End Function

    Sub Main()
        Console.WriteLine(Goo(Of Integer)()()())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="0")
        End Sub

    End Class
End Namespace
