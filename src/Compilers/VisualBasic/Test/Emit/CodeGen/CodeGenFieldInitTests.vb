' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenFieldInitTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestInstanceFieldInitializersPartialClass()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class D
    Public  Shared Sub Main()
        Dim p As [Partial]

        System.Console.WriteLine("Start Partial()")
        p = New [Partial]()
        System.Console.WriteLine("p.a = {0}", p.a)
        System.Console.WriteLine("p.b = {0}", p.b)
        System.Console.WriteLine("p.c = {0}", p.c)
        System.Console.WriteLine("End Partial()")

        System.Console.WriteLine("Start Partial(int)")
        p = New [Partial](2)
        System.Console.WriteLine("p.a = {0}", p.a)
        System.Console.WriteLine("p.b = {0}", p.b)
        System.Console.WriteLine("p.c = {0}", p.c)
        System.Console.WriteLine("End Partial(int)")
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Partial Class [Partial]
    Public a As Integer = D.Init(1, "Partial.a")

    Public Sub New()
    End Sub
End Class

Partial Class [Partial]
    Public c As Integer, b As Integer = D.Init(2, "Partial.b")

    Public Sub New(garbage As Integer)
        Me.c = D.Init(3, "Partial.c")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Start Partial()
Partial.a
Partial.b
p.a = 1
p.b = 2
p.c = 0
End Partial()
Start Partial(int)
Partial.a
Partial.b
Partial.c
p.a = 1
p.b = 2
p.c = 3
End Partial(int)
]]>)
        End Sub

        <Fact>
        Public Sub TestInstanceFieldInitializersInheritance()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class D
    Public Shared Sub Main()
        Dim d As New Derived2()
        System.Console.WriteLine("d.a = {0}", d.a)
        System.Console.WriteLine("d.b = {0}", d.b)
        System.Console.WriteLine("d.c = {0}", d.c)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Class Base
    Public a As Integer = D.Init(1, "Base.a")

    Public Sub New()
        System.Console.WriteLine("Base()")
    End Sub
End Class

Class Derived
    Inherits Base
    Public b As Integer = D.Init(2, "Derived.b")

    Public Sub New()
        System.Console.WriteLine("Derived()")
    End Sub
End Class

Class Derived2
    Inherits Derived
    Public c As Integer = D.Init(3, "Derived2.c")

    Public Sub New()
        System.Console.WriteLine("Derived2()")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Base.a
Base()
Derived.b
Derived()
Derived2.c
Derived2()
d.a = 1
d.b = 2
d.c = 3
]]>)
        End Sub

        <Fact>
        Public Sub TestStaticFieldInitializersPartialClass()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class D
    Public Shared Sub Main()
        System.Console.WriteLine("Partial.a = {0}", [Partial].a)
        System.Console.WriteLine("Partial.b = {0}", [Partial].b)
        System.Console.WriteLine("Partial.c = {0}", [Partial].c)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Partial Class [Partial]
    Public Shared a As Integer = D.Init(1, "Partial.a")
End Class

Partial Class [Partial]
    Public Shared c As Integer, b As Integer = D.Init(2, "Partial.b")

    Shared Sub New()
        c = D.Init(3, "Partial.c")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Partial.a
Partial.b
Partial.c
Partial.a = 1
Partial.b = 2
Partial.c = 3
]]>)
        End Sub

        <Fact>
        Public Sub TestStaticFieldInitializersInheritance1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class D
    Public Shared Sub Main()
        Dim b As New Base()
        System.Console.WriteLine("Base.a = {0}", Base.a)
        System.Console.WriteLine("Derived.b = {0}", Derived.b)
        System.Console.WriteLine("Derived2.c = {0}", Derived2.c)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Class Base
    Public Shared a As Integer = D.Init(1, "Base.a")
    Shared Sub New()
        System.Console.WriteLine("Base()")
    End Sub
End Class

Class Derived
    Inherits Base
    Public Shared b As Integer = D.Init(2, "Derived.b")
    Shared Sub New()
        System.Console.WriteLine("Derived()")
    End Sub
End Class

Class Derived2
    Inherits Derived
    Public Shared c As Integer = D.Init(3, "Derived2.c")
    Shared Sub New()
        System.Console.WriteLine("Derived2()")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Base.a
Base()
Base.a = 1
Derived.b
Derived()
Derived.b = 2
Derived2.c
Derived2()
Derived2.c = 3
]]>)
        End Sub

        <Fact>
        Public Sub TestStaticFieldInitializersInheritance2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class D
    Public Shared Sub Main()
        Dim b As Base = New Derived()
        System.Console.WriteLine("Base.a = {0}", Base.a)
        System.Console.WriteLine("Derived.b = {0}", Derived.b)
        System.Console.WriteLine("Derived2.c = {0}", Derived2.c)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Class Base
    Public Shared a As Integer = D.Init(1, "Base.a")
    Shared Sub New()
        System.Console.WriteLine("Base()")
    End Sub
End Class

Class Derived
    Inherits Base
    Public Shared b As Integer = D.Init(2, "Derived.b")
    Shared Sub New()
        System.Console.WriteLine("Derived()")
    End Sub
End Class

Class Derived2
    Inherits Derived
    Public Shared c As Integer = D.Init(3, "Derived2.c")
    Shared Sub New()
        System.Console.WriteLine("Derived2()")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Derived.b
Derived()
Base.a
Base()
Base.a = 1
Derived.b = 2
Derived2.c
Derived2()
Derived2.c = 3
]]>)
        End Sub

        <Fact>
        Public Sub TestStaticFieldInitializersInheritance3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class D
    Public Shared Sub Main()
        Dim b As Base = New Derived2()
        System.Console.WriteLine("Base.a = {0}", Base.a)
        System.Console.WriteLine("Derived.b = {0}", Derived.b)
        System.Console.WriteLine("Derived2.c = {0}", Derived2.c)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Class Base
    Public Shared a As Integer = D.Init(1, "Base.a")
    Shared Sub New()
        System.Console.WriteLine("Base()")
    End Sub
End Class

Class Derived
    Inherits Base
    Public Shared b As Integer = D.Init(2, "Derived.b")
    Shared Sub New()
        System.Console.WriteLine("Derived()")
    End Sub
End Class

Class Derived2
    Inherits Derived
    Public Shared c As Integer = D.Init(3, "Derived2.c")
    Shared Sub New()
        System.Console.WriteLine("Derived2()")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Derived2.c
Derived2()
Derived.b
Derived()
Base.a
Base()
Base.a = 1
Derived.b = 2
Derived2.c = 3
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldInitializersMixed()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim d As New Derived()
        System.Console.WriteLine("Base.a = {0}", Base.a)
        System.Console.WriteLine("Derived.b = {0}", Derived.b)
        System.Console.WriteLine("d.x = {0}", d.x)
        System.Console.WriteLine("d.y = {0}", d.y)

    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Class Base
    Public Shared a As Integer = C.Init(1, "Base.a")
    Public x As Integer = C.Init(3, "Base.x")

    Shared Sub New()
        System.Console.WriteLine("static Base()")
    End Sub

    Public Sub New()
        System.Console.WriteLine("Base()")
    End Sub
End Class

Class Derived
    Inherits Base
    Public Shared b As Integer = C.Init(2, "Derived.b")
    Public y As Integer = C.Init(4, "Derived.y")

    Shared Sub New()
        System.Console.WriteLine("static Derived()")
    End Sub

    Public Sub New()
        System.Console.WriteLine("Derived()")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Derived.b
static Derived()
Base.a
static Base()
Base.x
Base()
Derived.y
Derived()
Base.a = 1
Derived.b = 2
d.x = 3
d.y = 4
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldInitializersConstructorInitializers()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim a As New A()
        System.Console.WriteLine("a.a = {0}", a.a)
    End Sub

    Public Shared Function Init(value As Integer, message As String) As Integer
        System.Console.WriteLine(message)
        Return value
    End Function
End Class

Class A
    Public a As Integer = C.Init(1, "A.a")

    Public Sub New()
        Me.New(1)
        System.Console.WriteLine("A()")
    End Sub

    Public Sub New(garbage As Integer)
        System.Console.WriteLine("A(int)")
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
A.a
A(int)
A()
a.a = 1
]]>)
        End Sub

        <Fact>
        Public Sub TestFieldInitializersConstructorInitializers2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class A
    Protected x As Integer = 1
End Class

Class B
    Inherits A

    Private y As Integer = x

    Public Sub New()
        Console.WriteLine("x = " &amp; x &amp; ", y = " &amp; y)
    End Sub

    Public Shared Sub Main()
        Dim a As New B()
    End Sub
End Class
    </file>
</compilation>,
    expectedOutput:=<![CDATA[x = 1, y = 1]]>)
        End Sub

        <WorkItem(540460, "DevDiv")>
        <Fact>
        Public Sub TestStaticInitializerErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb">
Class C
    Public Shared F = G()
    Shared Sub G()
    End Sub
End Class
    </file>
</compilation>,
                references:=DefaultVbReferences,
                options:=TestOptions.ReleaseDll)

            Using executableStream As New MemoryStream()
                Dim result = compilation.Emit(executableStream)
                CompilationUtils.AssertTheseDiagnostics(result.Diagnostics,
        <expected>
BC30491: Expression does not produce a value.
    Public Shared F = G()
                      ~~~
</expected>)
            End Using
        End Sub

        <WorkItem(540460, "DevDiv")>
        <Fact>
        Public Sub TestInstanceInitializerErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb">
Class C
    Public F = G()
    Shared Sub G()
    End Sub
End Class
    </file>
</compilation>,
                references:=DefaultVbReferences,
                options:=TestOptions.ReleaseDll)

            Using executableStream As New MemoryStream()
                Dim result = compilation.Emit(executableStream)
                CompilationUtils.AssertTheseDiagnostics(result.Diagnostics,
        <expected>
BC30491: Expression does not produce a value.
    Public F = G()
               ~~~
</expected>)
            End Using
        End Sub

        <WorkItem(540467, "DevDiv")>
        <Fact>
        Public Sub TestCallNoParentheses()
            Dim source =
<compilation>
    <file name="c.vb">
Class C
    Shared Function M()
        Return 1
    End Function
    Public Shared F = M
    Public Shared G = M()
    Shared Sub Main()
        F = M
    End Sub
End Class
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:=<![CDATA[
]]>)
        End Sub

        <WorkItem(539286, "DevDiv")>
        <Fact>
        Public Sub TestLambdasInFieldInitializers()
            Dim source =
<compilation>
    <file name="c.vb">
Imports System

Class Class1(Of T)
    Dim f As Func(Of T, Integer, Integer) =
        Function(x, p)
            Dim a_outer As Integer = p * p
            Dim ff As Func(Of T, Integer, Integer) =
                Function(xx, pp)
                    If (xx IsNot Nothing) Then
                        Console.WriteLine(xx.GetType())
                    End If
                    Console.WriteLine(p * pp)
                    Return p
                End Function
            Return ff(x, p)
        End Function

    Public Function Foo() As Integer
        Return Nothing
    End Function

    Public Sub New()
        f(Nothing, 5)
    End Sub
    Public Sub New(p As T)
        f(p, 123)
    End Sub
End Class

Module Program
    Sub Main()
        Dim a As New Class1(Of DateTime)
        Dim b As New Class1(Of String)("abc")
    End Sub
End Module
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:=
            <![CDATA[
System.DateTime
25
System.String
15129]]>)
        End Sub

        <WorkItem(540603, "DevDiv")>
        <Fact>
        Public Sub TestAsNewInitializers()
            Dim source =
<compilation>
    <file name="c.vb">
Class Class1
    Dim f1 As New Object()
    Dim f2 As Object = New Object()
End Class
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source).
VerifyIL("Class1..ctor", <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  newobj     "Sub Object..ctor()"
  IL_000c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0011:  stfld      "Class1.f1 As Object"
  IL_0016:  ldarg.0
  IL_0017:  newobj     "Sub Object..ctor()"
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  stfld      "Class1.f2 As Object"
  IL_0026:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub FieldInitializerWithBadConstantValueSameModule()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Class A
    Public F As Integer = B.F1
End Class
Class B
    Public Const F1 As Integer = F2
    Public Shared F2 As Integer
End Class
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib(source)
            CompilationUtils.AssertTheseDiagnostics(compilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC30059: Constant expression is required.
    Public Const F1 As Integer = F2
                                 ~~
</expected>)
        End Sub

        <Fact>
        Public Sub FieldInitializerWithBadConstantValueDifferentModule()
            Dim source1 =
                <compilation name="1110a705-cc34-430b-9450-ca37031aa829">
                    <file name="c.vb"><![CDATA[
Option Strict On
Public Class B
    Public Const F1 As Integer = F2
    Public Shared F2 As Integer
End Class
]]>
                    </file>
                </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib(source1)
            compilation1.AssertTheseDiagnostics(<expected>
BC30059: Constant expression is required.
    Public Const F1 As Integer = F2
                                 ~~
</expected>)
            Dim source2 =
                <compilation name="2110a705-cc34-430b-9450-ca37031aa829">
                    <file name="c.vb"><![CDATA[
Option Strict On
Class A
    Public F As Object = M(B.F1)
    Private Shared Function M(i As Integer) As Object
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>
            Dim compilation2 = CreateCompilationWithMscorlibAndReferences(source2, {New VisualBasicCompilationReference(compilation1)})
            CompilationUtils.AssertTheseDiagnostics(compilation2.Emit(New MemoryStream()).Diagnostics,
<expected>
BC36970: Failed to emit module '2110a705-cc34-430b-9450-ca37031aa829.dll'.
</expected>)
        End Sub

    End Class

End Namespace
