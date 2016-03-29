' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
    Public Class ImplicitVariableTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub SimpleImplicitDeclaration()
            Dim compilation = CompileAndVerify(
<compilation name="SimpleImplicitDeclaration">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Sub Main()
        x = "Hello"
        dim y as string = "world"
        i% = 3
        While i &gt; 0
            Console.WriteLine("{0}, {1}", x, y)
            Console.WriteLine(i)
            i = i% - 1
        End While
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Hello, world
3
Hello, world
2
Hello, world
1
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(547017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547017")>
        <WorkItem(547018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547018")>
        <Fact>
        Public Sub SimpleImplicitDeclaration2()
            Dim compilation = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Explicit Off

Imports Microsoft.VisualBasic.FileSystem

Module Program
    Sub Main(args As String())
        If Microsoft.VisualBasic.InStr(CurDir(), "\") &lt;&gt; 0 Then
            sep$ = "\"
        Else
            sep$ = ":"
        End If

        Dim ResPath As String = CurDir() + sep$

        If "" &lt;&gt; "" Then
            On Error Resume Next
        End If
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:="")
            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ImplicitDeclarationDataFlow()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitDeclarationDataFlow">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Sub Main()
        Console.WriteLine(x)
        x = "Hello, world"
        Dim y, z As String
        While i% &lt; 4
            Console.WriteLine(x)
            Console.WriteLine(i)
            i = i% + 1
        End While
            z = "hi"
            Console.WriteLine(Y)
            Console.WriteLine(z)
        End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[

Hello, world
0
Hello, world
1
Hello, world
2
Hello, world
3

hi
]]>)

            compilation.VerifyDiagnostics(
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "x").WithArguments("x"),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "Y").WithArguments("y"))
        End Sub

        <Fact>
        Public Sub ImplicitWithReturnValueVariable()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitWithReturnValueVariable">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Function Foo(q As Integer) As Integer
        Foo = 10
        bar = Foo + q
        Foo = CInt(bar) + 7
    End Function

    Sub Main()
        i% = Foo(3)
        While i &gt; 0
            Console.Write("{0} ", i)
            i = i% - 1
        End While
        Console.WriteLine()
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
20 19 18 17 16 15 14 13 12 11 10 9 8 7 6 5 4 3 2 1
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ImplicitInLambda()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitInLambda">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Function Z(lam As Func(Of Integer, Integer)) As Integer
        r% = r% + 1
        Z = lam(4) + lam(1) + r%
    End Function

    Function Z(lam As Func(Of Long, Long)) As Integer
        Return CInt(lam(7) + lam(11))
    End Function

    Function Foo(p As Integer) As Integer
        Return Z(Function(z)
                     q% = q% + 1
                     Return z + q%
                 End Function)
    End Function

    Sub Main()
        Console.WriteLine(Foo(4))
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
9
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub InaccessibleVariable()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitInLambda">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Sub Main()
        Console.WriteLine(New C2().foo("hello"))
    End Sub
End Module

Class C1
    Private var As Integer
End Class

Class C2
    Inherits C1
    Public Function foo(x As String) As Object
        var = x
        Return var
    End Function
End Class    </file>
</compilation>,
    expectedOutput:=<![CDATA[
hello
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub NoImplicitDeclInInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoImplicitDeclInInitializer">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Imports System

Class C1
    Private var As Integer = foo
End Class
</file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'foo' is not declared. It may be inaccessible due to its protection level.
    Private var As Integer = foo
                             ~~~                                                   
</expected>)

        End Sub

        <Fact>
        Public Sub ImplicitInLambda2()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitInLambda2">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Module1
    Sub Main()
        Dim y As Integer = DirectCast(Function()
                                          x% = x% + 1
                                          Return x%
                                      End Function, Func(Of Integer)).Invoke() +
                           DirectCast(Function()
                                          x% = x% + 1
                                          Return x%
                                      End Function, Func(Of Integer)).Invoke()
        Console.WriteLine(y)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
3
]]>)

            compilation.VerifyDiagnostics()
        End Sub


        <Fact>
        Public Sub ImplicitInLambdaInInitializer()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitInLambdaInInitializer">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Module1
    Dim y As Integer = InvokeMultiple(Function()
                                          x% = x% + 1
                                          Return x%
                                      End Function, 2) +
                        InvokeMultiple(Function()
                                           x% = x% + 1
                                           Return x%
                                       End Function, 7)

    Function InvokeMultiple(f As Func(Of Integer), times As Integer) As Integer
        Dim result As Integer = 0
        For i As Integer = 1 To times
            result = f()
        Next
        Return result
    End Function

    Sub Main()
        Console.WriteLine(y)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
2
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ImplicitInLambdaInInitializer2()
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitInLambdaInInitializer">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Module1
    Dim y As Integer = InvokeMultiple(Function()
                                          dim a As Integer
                                          x% = x% + 1
                                          a = 5
                                          a = a + 1
                                          Console.WriteLine(x%)
                                          Console.WriteLine(a)
                                          Return x%
                                      End Function, 2)

    Function InvokeMultiple(f As Func(Of Integer), times As Integer) As Integer
        Dim result As Integer = 0
        For i As Integer = 1 To times
            result = f()
        Next
        Return result
    End Function

    Sub Main()
        Console.WriteLine(y)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
6
1
6
1
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ImplicitInSingleLineLambdaInInitializer()
            ' Note DEV10 does not alloc implicit variable declaration in Single line function lambda.
            ' This restriction is relaxed in Roslyn.
            Dim compilation = CompileAndVerify(
<compilation name="ImplicitInSingleLineLambdaInInitializer">
    <file name="a.vb">
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Module1
    Dim y As Integer = InvokeMultiple(Function() Increment(x%), 2) +
                       InvokeMultiple(Function() Increment(x%), 7)

    Function Increment(ByRef a As Integer) As Integer
        a = a + 1
        Return a
    End Function

    Function InvokeMultiple(f As Func(Of Integer), times As Integer) As Integer
        Dim result As Integer = 0
        For i As Integer = 1 To times
            result = f()
        Next
        Return result
    End Function

    Sub Main()
        Console.WriteLine(y)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
2
]]>)

            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub NoImplicitDeclInAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoImplicitDeclInInitializer">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Imports System

Class C1
    <Obsolete(zack)>
    Private var As Integer
End Class
]]>
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30451: 'zack' is not declared. It may be inaccessible due to its protection level.
    <Obsolete(zack)>
              ~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub NoImplicitDeclLeftOfDot()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoImplicitDeclLeftOfDot">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Imports System

Class C1
    Sub Foo()
        Dim x As String = y.ToString()
    End Sub
End Class
]]>
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30451: 'y' is not declared. It may be inaccessible due to its protection level.
        Dim x As String = y.ToString()
                          ~
]]></expected>)

        End Sub

        <Fact>
        Public Sub NoImplicitDeclInvocation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NoImplicitDeclInvocation">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Imports System

Class C1
    Sub Foo()
        Dim x As String = y()
    End Sub
End Class
]]>
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30451: 'y' is not declared. It may be inaccessible due to its protection level.
        Dim x As String = y()
                          ~
]]></expected>)

        End Sub

        <Fact()>
        Public Sub ImplicitInFor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="ImplicitInFor">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Option Infer On
Imports System

Class C1
            Sub Foo()
                For x = 1 To 10
                    x.GetTypeCode()
                Next
            End Sub
        End Class
]]>
    </file>
</compilation>)

            ' Inference in For should infer "integer" for x, even with option explicit off.
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact>
        Public Sub HidingEnclosingBlock1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="HidingEnclosingBlock1">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Option Infer On
Imports System

Module Module1
    Sub Main()
        x% = 10
        Dim y% = 0
        While y% < 10
            Dim x As String
            x = "hi"
            y% = y% + 1
        End While
    End Sub
End Module
]]>
    </file>
</compilation>)
            ' Simple case -- implicit variable declared before enclosed block 
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30616: Variable 'x' hides a variable in an enclosing block.
            Dim x As String
                ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub HidingEnclosingBlock2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="HidingEnclosingBlock2">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Option Infer On
Imports System

Module Module1
    Sub Main()
        Dim y% = 0
        While y% < 10
            Dim X As String
            x = "hi"
            y% = y% + 1
        End While
        x% = 10
    End Sub
End Module
]]>
    </file>
</compilation>)
            ' Tricky case -- implicit variable declared AFTER enclosed block 
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30616: Variable 'X' hides a variable in an enclosing block.
            Dim X As String
                ~
]]></expected>)
        End Sub


        <Fact>
        Public Sub HidingEnclosingBlock3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="HidingEnclosingBlock3">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Option Infer On
Imports System

Module Module1
    Sub Main()
        x% = 10
        y% = 0
        While y% < 10
            Dim q As Action(Of Integer) = Sub(x)
                                              Dim y As Integer = 4
                                              y = y + 1
                                          End Sub
        End While
    End Sub
End Module
]]>
    </file>
</compilation>)
            ' Simple case -- implicit variable declared before enclosed block 
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
            Dim q As Action(Of Integer) = Sub(x)
                                              ~
BC30616: Variable 'y' hides a variable in an enclosing block.
                                              Dim y As Integer = 4
                                                  ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub HidingEnclosingBlock4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="HidingEnclosingBlock4">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Option Infer On
Imports System

Module Module1
    Sub Main()
        While y% < 10
            Dim q As Action(Of Integer) = Sub(x)
                                              Dim y As Integer = 4
                                              y = y + 1
                                          End Sub
        End While
        x% = 10
        y% = 0
    End Sub
End Module
]]>
    </file>
</compilation>)
            ' Trick case -- implicit variable declared after enclosed block 
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
            Dim q As Action(Of Integer) = Sub(x)
                                              ~
BC30616: Variable 'y' hides a variable in an enclosing block.
                                              Dim y As Integer = 4
                                                  ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ReservedNames()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="HidingEnclosingBlock4">
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Sub Main()
        x = Null
        y = Rnd()
        z = Empty
        q = r
    End Sub
End Module
]]>
    </file>
</compilation>)
            ' Dev10 disallows implicit variable creation for certain names.
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30451: 'Null' is not declared. It may be inaccessible due to its protection level.
        x = Null
            ~~~~
BC30451: 'Rnd' is not declared. It may be inaccessible due to its protection level.
        y = Rnd()
            ~~~
BC30389: 'System.Empty' is not accessible in this context because it is 'Friend'.
        z = Empty
            ~~~~~
BC42104: Variable 'r' is used before it has been assigned a value. A null reference exception could result at runtime.
        q = r
            ~
]]></expected>)
        End Sub

        <WorkItem(542455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542455")>
        <Fact>
        Public Sub VariableAcrossIfParts()
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict Off

Module Module1

            Sub Main()
                If y > 10 Then
                    y = ">10" + x
                ElseIf y < 10 Then
                    y = "<10" + x
                End If
            End Sub

End Module]]>
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "y").WithArguments("y"),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "x").WithArguments("x"))
        End Sub

        <WorkItem(542455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542455")>
        <Fact>
        Public Sub VariableAcrossIfParts2()
            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Explicit Off
Option Strict Off

Module Module1

            Sub Main()
                If y > 10 Then
                    a = ">10" + x
                ElseIf z < 10 Then
                    b = "<10" + x
                End If
            End Sub

End Module]]>
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "y").WithArguments("y"),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "x").WithArguments("x"),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "z").WithArguments("z"))
        End Sub

        <WorkItem(542530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542530")>
        <Fact>
        Public Sub LambdaBindingOrder()
            Dim compilation = CompileAndVerify(
<compilation name="SimpleImplicitDeclaration">
    <file name="a.vb">
Option Explicit Off
Option Strict On

Imports System

Module Module1
    Sub f(x As Integer, y As Integer)
        Console.WriteLine("hello")
    End Sub

    Sub g(x As Func(Of Integer), y As Integer)
        Console.WriteLine("world")
    End Sub

    Sub Main()
        f(a%, a)
        g(Function() b + c%, b%)   ' this should be error, but isn't
        Console.WriteLine("done")
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
hello
world
done
]]>)

            ' This should be an error (because b is declared implicitly without a type character
            ' first), but isn't in both Dev10 and Roslyn.

            compilation.VerifyDiagnostics()
        End Sub



#Region "GetSemanticInfo Tests"

        <Fact>
        Public Sub BindImplicitVariableAsLeftHandSideOfAssignment()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                x = 1'BIND:"x"
            ]]>)
        End Sub

        <Fact>
        Public Sub BindImplicitVariableAsRightHandSideOfAssignment()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                y = x'BIND:"x"
            ]]>)
        End Sub

        <Fact>
        Public Sub BindImplicitVariableInExpression()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                y = y Is x'BIND:"x"
            ]]>)
        End Sub

        <Fact>
        Public Sub BindImplicitVariableAsMethodArgument()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                System.Console.WriteLine(x)'BIND:"x"
            ]]>)
        End Sub

        <Fact>
        Public Sub BindImplicitVariableInSingleLineLambda()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                Dim f As Func(Of Object) = Function() x'BIND:"x"
            ]]>)
        End Sub

        <Fact>
        Public Sub BindImplicitVariableInMultiLineLambda()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                Dim f As Func(Of Object) = Function()
                                               Return x'BIND:"x"
                                           End Function
            ]]>)
        End Sub

        <Fact>
        Public Sub BindImplicitVariableWhenConflictsWithExplicitVariable()
            VerifyImplicitDeclarationSemanticInfo(<![CDATA[
                If True Then
                    Dim x = 1
                End If
                x = 1'BIND:"x"
            ]]>)
        End Sub

#End Region

#Region "BindExpression Tests"

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub SpeculativeBindImplicitVariableAsLeftHandSideOfOfAssignment()
            VerifyImplicitDeclarationBindExpression(<![CDATA[
                'BIND
            ]]>,
            expression:="x",
            expectedTypeName:="System.Object",
            symbolKind:=SymbolKind.Local)
        End Sub

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub SpeculativeBindImplicitVariableAsMethodArgument()
            VerifyImplicitDeclarationBindExpression(<![CDATA[
                'BIND
            ]]>,
            expression:="System.Console.WriteLine(x)",
           expectedTypeName:="System.Void",
           symbolKind:=SymbolKind.Method)
        End Sub

        <Fact>
        Public Sub SpeculativeBindImplicitVariableInSingleLineLambda()
            VerifyImplicitDeclarationBindExpression(<![CDATA[
                Dim f As Func(Of Object) = Function() x'BIND
            ]]>,
            expression:="y Is x",
            expectedTypeName:="System.Boolean")
        End Sub

        <Fact>
        Public Sub SpeculativeBindImplicitVariableWhenConflictsWithExplicitVariable()
            VerifyImplicitDeclarationBindExpression(<![CDATA[
                If True Then
                    Dim x = 1
                End If
                'BIND
            ]]>,
            expression:="x = 1",
            expectedTypeName:="System.Object",
            symbolKind:=SymbolKind.Method,
            expectedSymbol:="Function System.Object.op_Equality(left As System.Object, right As System.Object) As System.Object")
        End Sub

        <Fact>
        Public Sub SpeculativeBindImplicitVariableDeclarationInOuterScope()
            VerifyImplicitDeclarationBindExpression(<![CDATA[
                If True Then
                    x% = 1
                End If
                'BIND
            ]]>,
            expression:="x + 1",
            expectedTypeName:="System.Int32",
            symbolKind:=SymbolKind.Method,
            expectedSymbol:="Function System.Int32.op_Addition(left As System.Int32, right As System.Int32) As System.Int32")
        End Sub

#End Region

#Region "LookupSymbols Tests"

        <Fact>
        Public Sub LookupImplicitVariableOnRightHandSideOfAssignment()
            VerifyImplicitDeclarationLookupSymbols(<![CDATA[
                x = 
                    1'BIND:"1"
            ]]>,
            expected:={"x"})
        End Sub

        <Fact>
        Public Sub LookupImplicitVariableInExpression()
            VerifyImplicitDeclarationLookupSymbols(<![CDATA[
                x = y Is 
                    o'BIND:"o"
            ]]>,
            expected:={"x", "y"})
        End Sub

        <Fact>
        Public Sub LookupImplicitVariableInSingleLineLambda()
            VerifyImplicitDeclarationLookupSymbols(<![CDATA[
                Dim f As Func(Of Object) = Function() 1'BIND:"1"
                x = 1
            ]]>,
            expected:={"x"})
        End Sub

        <Fact>
        Public Sub LookupImplicitVariableDeclaredInInnerScope()
            VerifyImplicitDeclarationLookupSymbols(<![CDATA[
                If True Then
                    x = 1
                End If
                y = 1'BIND:"y"
            ]]>,
            expected:={"x"})
        End Sub

        <Fact>
        Public Sub LookupImplicitVariableDeclaredInOuterScope()
            VerifyImplicitDeclarationLookupSymbols(<![CDATA[
                If True Then
                    y = y'BIND:"y"
                End If
                x = 1
            ]]>,
            expected:={"x"})
        End Sub

        <WorkItem(1036381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036381")>
        <Fact>
        Public Sub Bug1036381_01()
            Dim source =
<compilation>
    <file name="a.vb">
Option Explicit Off
 
Module Program
    Sub Main(args As String())
        x = 1
        Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntime(source)

            Dim tree = compilation1.SyntaxTrees.Single()
            Dim model = compilation1.GetSemanticModel(tree)
            Dim main1 = tree.GetRoot().DescendantNodes().OfType(Of MethodBlockBaseSyntax)().Single()
            Dim position = main1.Statements.First.SpanStart

            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim main2 = compilation2.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType(Of MethodBlockBaseSyntax)().Single()

            Dim speculative As SemanticModel = Nothing
            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(position, main2, speculative))

            Dim l1 = speculative.LookupSymbols(position, name:="x").Single()

            Assert.NotNull(l1)
            Assert.Equal(SymbolKind.Local, l1.Kind)

            Dim l2 = model.LookupSymbols(position, name:="x").Single()

            Assert.NotNull(l2)
            Assert.Equal(SymbolKind.Local, l2.Kind)
            Assert.NotEqual(l1, l2)
        End Sub

        <WorkItem(1036381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036381")>
        <Fact>
        Public Sub Bug1036381_02()
            Dim source =
<compilation>
    <file name="a.vb">
Option Explicit Off
 
Module Program
    Sub Main(args As String())
        x = 1
        Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntime(source)

            Dim tree = compilation1.SyntaxTrees.Single()
            Dim model = compilation1.GetSemanticModel(tree)
            Dim main1 = tree.GetRoot().DescendantNodes().OfType(Of MethodBlockBaseSyntax)().Single()
            Dim position = main1.Statements.First.SpanStart

            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim main2 = compilation2.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType(Of MethodBlockBaseSyntax)().Single()

            Dim speculative As SemanticModel = Nothing
            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(position, main2, speculative))

            Dim l2 = model.LookupSymbols(position, name:="x").Single()

            Assert.NotNull(l2)
            Assert.Equal(SymbolKind.Local, l2.Kind)

            Dim l1 = speculative.LookupSymbols(position, name:="x").Single()

            Assert.NotNull(l1)
            Assert.Equal(SymbolKind.Local, l1.Kind)

            Assert.NotEqual(l1, l2)
        End Sub

#End Region

#Region "Helpers"

        Friend Shared Function GetSourceXElementFromTemplate(code As XCData) As XElement
            Return <compilation>
                       <file name="a.vb">
Option Infer On
Option Explicit Off
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main()
<%= code.Value %>
    End Sub
End Module
                       </file>
                   </compilation>
        End Function

        Private Sub VerifySemanticInfo(semanticInfo As SemanticInfoSummary, expectedType As TypeSymbol, symbolKind? As SymbolKind, expectedSymbol As String)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(expectedType, semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.False(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            If symbolKind.HasValue Then
                Assert.Equal(symbolKind, semanticInfo.Symbol.Kind)

                If expectedSymbol IsNot Nothing Then
                    Assert.Equal(expectedSymbol, semanticInfo.Symbol.ToTestDisplayString())
                End If
            Else
                Assert.Null(semanticInfo.Symbol)
            End If

            Assert.Equal(expectedType, semanticInfo.Type)
        End Sub

        Private Sub VerifySemanticInfoSummary(semanticInfo As SemanticInfoSummary, expectedType As TypeSymbol, symbolKind? As SymbolKind)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(expectedType, semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.False(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            If symbolKind.HasValue Then
                Assert.Equal(symbolKind, semanticInfo.Symbol.Kind)
            Else
                Assert.Null(semanticInfo.Symbol)
            End If

            Assert.Equal(expectedType, semanticInfo.Type)
        End Sub

        Private Sub VerifyImplicitDeclarationSemanticInfo(code As XCData)
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(GetSourceXElementFromTemplate(code))
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim objectSymbol = compilation.GetTypeByMetadataName("System.Object")

            VerifySemanticInfoSummary(semanticInfo, objectSymbol, SymbolKind.Local)
        End Sub

        Private Sub VerifyImplicitDeclarationBindExpression(code As XCData, expression As String, expectedTypeName As String, Optional symbolKind? As SymbolKind = Nothing, Optional expectedSymbol As String = Nothing)
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(GetSourceXElementFromTemplate(code))
            Dim tree = compilation.SyntaxTrees.Where(Function(t) t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position = CompilationUtils.FindPositionFromText(tree, "'BIND")
            Dim semanticInfo As SemanticInfoSummary = semanticModel.GetSpeculativeSemanticInfoSummary(position, SyntaxFactory.ParseExpression(expression), SpeculativeBindingOption.BindAsExpression)

            VerifySemanticInfo(semanticInfo, compilation.GetTypeByMetadataName(expectedTypeName), symbolKind, expectedSymbol)
        End Sub

        Private Sub VerifyImplicitDeclarationLookupSymbols(code As XCData, Optional expected() As String = Nothing, Optional notExpected() As String = Nothing)
            If expected Is Nothing AndAlso notExpected Is Nothing Then
                Throw New ArgumentException("Must specify a value for either 'expected' or 'notExpected'")
            End If

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(GetSourceXElementFromTemplate(code))
            Dim actual = GetLookupSymbols(compilation, "a.vb").Select(Function(s) s.Name)

            For Each s In If(expected, {})
                Assert.Contains(s, actual)
            Next
            For Each s In If(notExpected, {})
                Assert.DoesNotContain(s, actual)
            Next
        End Sub

#End Region

    End Class

End Namespace

