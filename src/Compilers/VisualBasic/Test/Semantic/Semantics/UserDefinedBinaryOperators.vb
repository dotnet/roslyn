' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class UserDefinedBinaryOperators
        Inherits BasicTestBase

        <Fact>
        Public Sub BasicTest()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Public Class B2

    Public Shared Operator +(x As B2, y As B2) As B2 
        System.Console.WriteLine("+")
        Return x
    End Operator

    Public Shared Operator -(x As B2, y As B2) As B2 
        System.Console.WriteLine("-")
        Return x
    End Operator

    Public Shared Operator *(x As B2, y As B2) As B2 
        System.Console.WriteLine("*")
        Return x
    End Operator

    Public Shared Operator /(x As B2, y As B2) As B2 
        System.Console.WriteLine("/")
        Return x
    End Operator

    Public Shared Operator \(x As B2, y As B2) As B2 
        System.Console.WriteLine("\")
        Return x
    End Operator

    Public Shared Operator Mod(x As B2, y As B2) As B2 
        System.Console.WriteLine("Mod")
        Return x
    End Operator

    Public Shared Operator ^(x As B2, y As B2) As B2 
        System.Console.WriteLine("^")
        Return x
    End Operator

    Public Shared Operator =(x As B2, y As B2) As B2 
        System.Console.WriteLine("=")
        Return x
    End Operator

    Public Shared Operator <>(x As B2, y As B2) As B2
        System.Console.WriteLine("<>")
        Return x
    End Operator

    Public Shared Operator <(x As B2, y As B2) As B2 
        System.Console.WriteLine("<")
        Return x
    End Operator

    Public Shared Operator >(x As B2, y As B2) As B2 
        System.Console.WriteLine(">")
        Return x
    End Operator

    Public Shared Operator <=(x As B2, y As B2) As B2
        System.Console.WriteLine("<=")
        Return x
    End Operator

    Public Shared Operator >=(x As B2, y As B2) As B2
        System.Console.WriteLine(">=")
        Return x
    End Operator

    Public Shared Operator Like(x As B2, y As B2) As B2
        System.Console.WriteLine("Like")
        Return x
    End Operator

    Public Shared Operator &(x As B2, y As B2) As B2 
        System.Console.WriteLine("&")
        Return x
    End Operator

    Public Shared Operator And(x As B2, y As B2) As B2
        System.Console.WriteLine("And")
        Return x
    End Operator

    Public Shared Operator Or(x As B2, y As B2) As B2 
        System.Console.WriteLine("Or")
        Return x
    End Operator

    Public Shared Operator Xor(x As B2, y As B2) As B2
        System.Console.WriteLine("Xor")
        Return x
    End Operator

    Public Shared Operator <<(x As B2, y As Integer) As B2
        System.Console.WriteLine("<<")
        Return x
    End Operator

    Public Shared Operator >>(x As B2, y As Integer) As B2
        System.Console.WriteLine(">>")
        Return x
    End Operator
End Class

Module Module1

    Sub Main() 
        Dim x, y As New B2()
        Dim r As B2
        r = x + y      'BIND1:"x + y"
        r = x - y      'BIND2:"x - y"
        r = x * y      'BIND3:"x * y"
        r = x / y      'BIND4:"x / y"
        r = x \ y      'BIND5:"x \ y"
        r = x Mod y    'BIND6:"x Mod y"
        r = x ^ y      'BIND7:"x ^ y"
        r = x = y      'BIND8:"x = y"
        r = x <> y     'BIND9:"x <> y"
        r = x < y      'BIND10:"x < y"
        r = x > y      'BIND11:"x > y"
        r = x <= y     'BIND12:"x <= y"
        r = x >= y     'BIND13:"x >= y"
        r = x Like y   'BIND14:"x Like y"
        r = x & y      'BIND15:"x & y"
        r = x And y    'BIND16:"x And y"
        r = x Or y     'BIND17:"x Or y"
        r = x Xor y    'BIND18:"x Xor y"
        r = x << 2     'BIND19:"x << 2"
        r = x >> 3     'BIND20:"x >> 3"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+
-
*
/
\
Mod
^
=
<>
<
>
<=
>=
Like
&
And
Or
Xor
<<
>>
]]>)

            Dim baseLine() As String = {
        "Public Shared Operator +(x As B2, y As B2) As B2",
        "Public Shared Operator -(x As B2, y As B2) As B2",
        "Public Shared Operator *(x As B2, y As B2) As B2",
        "Public Shared Operator /(x As B2, y As B2) As B2",
        "Public Shared Operator \(x As B2, y As B2) As B2",
        "Public Shared Operator Mod(x As B2, y As B2) As B2",
        "Public Shared Operator ^(x As B2, y As B2) As B2",
        "Public Shared Operator =(x As B2, y As B2) As B2",
        "Public Shared Operator <>(x As B2, y As B2) As B2",
        "Public Shared Operator <(x As B2, y As B2) As B2",
        "Public Shared Operator >(x As B2, y As B2) As B2",
        "Public Shared Operator <=(x As B2, y As B2) As B2",
        "Public Shared Operator >=(x As B2, y As B2) As B2",
        "Public Shared Operator Like(x As B2, y As B2) As B2",
        "Public Shared Operator &(x As B2, y As B2) As B2",
        "Public Shared Operator And(x As B2, y As B2) As B2",
        "Public Shared Operator Or(x As B2, y As B2) As B2",
        "Public Shared Operator Xor(x As B2, y As B2) As B2",
        "Public Shared Operator <<(x As B2, y As Integer) As B2",
        "Public Shared Operator >>(x As B2, y As Integer) As B2"
                }

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            For i As Integer = 0 To 20 - 1
                Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", i + 1)
                Dim symbolInfo = model.GetSymbolInfo(node)
                Assert.Equal(baseLine(i), symbolInfo.Symbol.ToDisplayString())
            Next
        End Sub

        <Fact>
        Public Sub ShortCircuiting1()
            Dim compilationDef =
<compilation name="ShortCircuiting1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Class B3

    Private m_Value As Boolean

    Public Sub New(x As Boolean)
        System.Console.WriteLine("New B3(" & x & ")")
        m_Value = x
    End Sub

    Public Overrides Function ToString() As String
        Return "B3(" & m_Value & ")"
    End Function

    Public Shared Operator IsTrue(x As B3) As Boolean
        System.Console.WriteLine("IsTrue")
        Return x.m_Value
    End Operator

    Public Shared Operator IsFalse(x As B3) As Boolean
        System.Console.WriteLine("IsFalse")
        Return Not x.m_Value
    End Operator

    Public Shared Operator And(x As B3, y As B3) As B3
        System.Console.Write("And ")
        Return New B3(x.m_Value And y.m_Value)
    End Operator

    Public Shared Operator Or(x As B3, y As B3) As B3
        System.Console.Write("Or ")
        Return New B3(x.m_Value Or y.m_Value)
    End Operator

End Class

Module Module1

    Sub Main() 
        System.Console.WriteLine(New B3(False) AndAlso New B3(False)) 'BIND1:"New B3(False) AndAlso New B3(False)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(False) AndAlso New B3(True)) 'BIND2:"New B3(False) AndAlso New B3(True)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(True) AndAlso New B3(False)) 'BIND3:"New B3(True) AndAlso New B3(False)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(True) AndAlso New B3(True)) 'BIND4:"New B3(True) AndAlso New B3(True)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(False) OrElse New B3(False)) 'BIND5:"New B3(False) OrElse New B3(False)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(False) OrElse New B3(True)) 'BIND6:"New B3(False) OrElse New B3(True)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(True) OrElse New B3(False)) 'BIND7:"New B3(True) OrElse New B3(False)"
        System.Console.WriteLine("----")
        System.Console.WriteLine(New B3(True) OrElse New B3(True)) 'BIND8:"New B3(True) OrElse New B3(True)"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
New B3(False)
IsFalse
B3(False)
----
New B3(False)
IsFalse
B3(False)
----
New B3(True)
IsFalse
New B3(False)
And New B3(False)
B3(False)
----
New B3(True)
IsFalse
New B3(True)
And New B3(True)
B3(True)
----
New B3(False)
IsTrue
New B3(False)
Or New B3(False)
B3(False)
----
New B3(False)
IsTrue
New B3(True)
Or New B3(True)
B3(True)
----
New B3(True)
IsTrue
B3(True)
----
New B3(True)
IsTrue
B3(True)
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            For i As Integer = 0 To 8 - 1
                Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", i + 1)
                Dim symbolInfo = model.GetSymbolInfo(node)
                Assert.Null(symbolInfo.Symbol)
            Next
        End Sub

        <Fact>
        Public Sub ShortCircuiting2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B3

        Public Shared Operator IsTrue(x As B3) As Boolean
            Return True
        End Operator

        Public Shared Operator IsFalse(x As B3) As Boolean
            Return False
        End Operator

        Public Shared Operator And(x As B3, y As B3) As B4
            Return Nothing
        End Operator

        Public Shared Operator Or(x As B3, y As B3) As B4
            Return Nothing
        End Operator

    End Class

    Class B4
    End Class

    Sub Main()
        Dim x As Object

        x = New B3() AndAlso New B3() 'BIND1:"New B3() AndAlso New B3()"
        x = New B3() OrElse New B3() 'BIND2:"New B3() OrElse New B3()"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33034: Return and parameter types of 'Public Shared Operator And(x As Module1.B3, y As Module1.B3) As Module1.B4' must be 'Module1.B3' to be used in a 'AndAlso' expression.
        x = New B3() AndAlso New B3() 'BIND1:"New B3() AndAlso New B3()"
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC33034: Return and parameter types of 'Public Shared Operator Or(x As Module1.B3, y As Module1.B3) As Module1.B4' must be 'Module1.B3' to be used in a 'OrElse' expression.
        x = New B3() OrElse New B3() 'BIND2:"New B3() OrElse New B3()"
            ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 0 To 2 - 1
                Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i + 1)
                Dim symbolInfo = model.GetSymbolInfo(node)
                Assert.Null(symbolInfo.Symbol)
            Next
        End Sub

        <Fact>
        Public Sub ShortCircuiting3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B3

        Public Shared Operator And(x As B3, y As B3) As B3
            Return Nothing
        End Operator

        Public Shared Operator Or(x As B3, y As B3) As B3
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As Object

        x = New B3() AndAlso New B3() 'BIND1:"New B3() AndAlso New B3()"
        x = New B3() OrElse New B3() 'BIND2:"New B3() OrElse New B3()"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33035: Type 'Module1.B3' must define operator 'IsFalse' to be used in a 'AndAlso' expression.
        x = New B3() AndAlso New B3() 'BIND1:"New B3() AndAlso New B3()"
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC33035: Type 'Module1.B3' must define operator 'IsTrue' to be used in a 'OrElse' expression.
        x = New B3() OrElse New B3() 'BIND2:"New B3() OrElse New B3()"
            ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 0 To 2 - 1
                Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i + 1)
                Dim symbolInfo = model.GetSymbolInfo(node)
                Assert.Null(symbolInfo.Symbol)
            Next
        End Sub

        <Fact>
        Public Sub ShortCircuiting4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B2
        Public Shared Operator IsTrue(x As B2) As Boolean
            Return True
        End Operator

        Public Shared Operator IsFalse(x As B2) As Boolean
            Return False
        End Operator
    End Class

    Class B3
        Inherits B2

        Public Shared Operator And(x As B3, y As B3) As B3
            Return Nothing
        End Operator

        Public Shared Operator Or(x As B3, y As B3) As B3
            Return Nothing
        End Operator

    End Class

    Sub Main()
        Dim x As Object

        x = New B3() AndAlso New B3() 'BIND1:"New B3() AndAlso New B3()"
        x = New B3() OrElse New B3() 'BIND2:"New B3() OrElse New B3()"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30452: Operator 'AndAlso' is not defined for types 'Module1.B3' and 'Module1.B3'.
        x = New B3() AndAlso New B3() 'BIND1:"New B3() AndAlso New B3()"
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30452: Operator 'OrElse' is not defined for types 'Module1.B3' and 'Module1.B3'.
        x = New B3() OrElse New B3() 'BIND2:"New B3() OrElse New B3()"
            ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 0 To 2 - 1
                Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i + 1)
                Dim symbolInfo = model.GetSymbolInfo(node)
                Assert.Null(symbolInfo.Symbol)
            Next
        End Sub

        <Fact>
        Public Sub ShortCircuiting5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B2
        Public Shared Operator And(x As B3, y As B2) As B2
            Return Nothing
        End Operator

        Public Shared Operator Or(x As B3, y As B2) As B2
            Return Nothing
        End Operator
    End Class

    Class B3
        Public Shared Operator And(x As B3, y As B2) As B3
            Return Nothing
        End Operator

        Public Shared Operator Or(x As B3, y As B2) As B3
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x As Object

        x = New B3() AndAlso New B2() 'BIND1:"New B3() AndAlso New B2()"
        x = New B3() OrElse New B2() 'BIND2:"New B3() OrElse New B2()"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'And' is most specific for these arguments:
    'Public Shared Operator Module1.B3.And(x As Module1.B3, y As Module1.B2) As Module1.B3': Not most specific.
    'Public Shared Operator Module1.B2.And(x As Module1.B3, y As Module1.B2) As Module1.B2': Not most specific.
        x = New B3() AndAlso New B2() 'BIND1:"New B3() AndAlso New B2()"
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30521: Overload resolution failed because no accessible 'Or' is most specific for these arguments:
    'Public Shared Operator Module1.B3.Or(x As Module1.B3, y As Module1.B2) As Module1.B3': Not most specific.
    'Public Shared Operator Module1.B2.Or(x As Module1.B3, y As Module1.B2) As Module1.B2': Not most specific.
        x = New B3() OrElse New B2() 'BIND2:"New B3() OrElse New B2()"
            ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 0 To 2 - 1
                Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", i + 1)
                Dim symbolInfo = model.GetSymbolInfo(node)
                Assert.Null(symbolInfo.Symbol)
            Next
        End Sub

        <Fact>
        Public Sub OperatorMapping1_LogicalAndUnsignedShiftOnly()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A16
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A16  op_LogicalAnd(class A16 x,
                                   class A16 y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A16 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalAnd"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A16::op_LogicalAnd

  .method public hidebysig specialname static 
          class A16  op_LogicalOr(class A16 x,
                                  class A16 y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A16 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalOr"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A16::op_LogicalOr

  .method public hidebysig specialname static 
          class A16  op_UnsignedLeftShift(class A16 x,
                                          int32 y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A16 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_UnsignedLeftShift"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A16::op_UnsignedLeftShift

  .method public hidebysig specialname static 
          class A16  op_UnsignedRightShift(class A16 x,
                                           int32 y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A16 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_UnsignedRightShift"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A16::op_UnsignedRightShift

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A16::.ctor

} // end of class A16
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim x As Object

        x = New A16() And New A16()
        x = New A16() Or New A16()
        x = New A16() << 1
        x = New A16() >> 2
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
op_LogicalAnd
op_LogicalOr
op_UnsignedLeftShift
op_UnsignedRightShift
]]>)
        End Sub

        <Fact>
        Public Sub OperatorMapping_BothBitwiseAndLogical()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  br.s       IL_0008

    IL_0008:  ret
  } // end of method C::.ctor

  .method public hidebysig specialname static 
          class C  op_LogicalAnd(class C x,
                                 class C y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalAnd"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_LogicalAnd

  .method public hidebysig specialname static 
          class C  op_BitwiseAnd(class C x,
                                 class C y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_BitwiseAnd"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_BitwiseAnd

  .method public hidebysig specialname static 
          class C  op_BitwiseOr(class C x,
                                class C y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_BitwiseOr"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_BitwiseOr

  .method public hidebysig specialname static 
          class C  op_LogicalOr(class C x,
                                class C y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalOr"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_LogicalOr

  .method public hidebysig specialname static 
          class C  op_LogicalNot(class C x) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      " op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_LogicalNot

  .method public hidebysig specialname static 
          class C  op_OnesComplement(class C x) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_OnesComplement

} // end of class C
]]>

            Dim compilationDef =
<compilation name="BothBitwiseAndLogical">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim c As New C()
        c = New C() And (New C() or c) And Not c Or Not New C()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
op_BitwiseOr
op_BitwiseAnd
op_OnesComplement
op_BitwiseAnd
op_OnesComplement
op_BitwiseOr
]]>)
        End Sub

        <Fact>
        Public Sub OperatorMapping_BothSignedAndUnsignedShift()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  br.s       IL_0008

    IL_0008:  ret
  } // end of method C::.ctor

  .method public hidebysig specialname static 
          class C  op_LeftShift(class C x,
                                int32 y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LeftShift"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_LeftShift

  .method public hidebysig specialname static 
          class C  op_UnsignedLeftShift(class C x,
                                int32 y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_UnsignedLeftShift"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_UnsignedLeftShift

  .method public hidebysig specialname static 
          class C  op_UnsignedRightShift(class C x,
                                 int32 y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_UnsignedRightShift"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_UnsignedRightShift

  .method public hidebysig specialname static 
          class C  op_RightShift(class C x,
                                 int32 y) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] class C V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "op_RightShift"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  ldarg.0
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method C::op_RightShift

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C::Main

} // end of class C
]]>

            Dim compilationDef =
<compilation name="BothSignedAndUnsignedShift">
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Module Module1
    Sub Main()
        Dim c As New C()
        c = (New C << 1) >> 2 << 3
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
op_LeftShift
op_RightShift
op_LeftShift
]]>)
        End Sub

        <Fact>
        Public Sub Lifted1()
            Dim compilationDef =
<compilation name="Lifted1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As S1) As S1
            System.Console.WriteLine("+(x As S1, y As S1)")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        System.Console.WriteLine(New S1?() + New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?() + New S1?(New S1()))
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) + New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) + New S1?(New S1()))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[

----

----

----
+(x As S1, y As S1)
Module1+S1
]]>)
        End Sub

        <Fact>
        Public Sub Lifted2()
            Dim compilationDef =
<compilation name="Lifted2">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Sub Main()
        If New Integer?() = New Integer?() Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        If New Integer?() = New Integer?(New Integer()) Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        If New Integer?(New Integer()) = New Integer?() Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If

        If New Integer?(New Integer()) = New Integer?(New Integer()) Then
            System.Console.WriteLine("If")
        Else
            System.Console.WriteLine("Else")
        End If
    End Sub

    Sub Test()
        Dim x1 As Integer?
        Dim x2 As Integer?

        If x1 = x2 Then
        End If
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Else
Else
Else
If
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub Lifted3()
            Dim compilationDef =
<compilation name="Lifted3">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1
        Public Shared Operator And(x As S1, y As S1) As S1
            System.Console.WriteLine("And(x As S1, y As S1) As S1")
            Return x
        End Operator

        Public Shared Operator IsFalse(x As S1) As Boolean
            System.Console.WriteLine("IsFalse(x As S1) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsTrue(x As S1) As Boolean
            System.Console.WriteLine("IsTrue(x As S1) As Boolean")
            Return True
        End Operator
    End Structure

    Sub Main()
        System.Console.WriteLine(New S1?() AndAlso New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?() AndAlso New S1?(New S1()))
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) AndAlso New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) AndAlso New S1?(New S1()))
    End Sub

    Sub Test()
        Dim x1 As S1?
        Dim x2 As S1?
        
        If x1 AndAlso x2 Then
        End If

        Dim y1 As S1
        Dim y2 As S1
        
        If y1 AndAlso y2 Then
        End If
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[

----

----
IsFalse(x As S1) As Boolean

----
IsFalse(x As S1) As Boolean
And(x As S1, y As S1) As S1
Module1+S1
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub Lifted4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1
        Public Shared Operator And(x As S1, y As S1) As S1
            System.Console.WriteLine("And(x As S1, y As S1) As S1")
            Return x
        End Operator

        Public Shared Operator IsFalse(x As S1?) As Boolean
            System.Console.WriteLine("IsFalse(x As S1) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsTrue(x As S1?) As Boolean
            System.Console.WriteLine("IsTrue(x As S1) As Boolean")
            Return True
        End Operator
    End Structure

    Sub Main()
        System.Console.WriteLine(New S1?() AndAlso New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?() AndAlso New S1?(New S1()))
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) AndAlso New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) AndAlso New S1?(New S1()))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
IsFalse(x As S1) As Boolean

----
IsFalse(x As S1) As Boolean

----
IsFalse(x As S1) As Boolean

----
IsFalse(x As S1) As Boolean
And(x As S1, y As S1) As S1
Module1+S1
]]>)

        End Sub

        <Fact>
        Public Sub Lifted5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1
        Public Shared Operator And(x As S1?, y As S1?) As S1?
            System.Console.WriteLine("And(x As S1?, y As S1?) As S1?")
            Return x
        End Operator

        Public Shared Operator IsFalse(x As S1) As Boolean
            System.Console.WriteLine("IsFalse(x As S1) As Boolean")
            Return False
        End Operator

        Public Shared Operator IsTrue(x As S1) As Boolean
            System.Console.WriteLine("IsTrue(x As S1) As Boolean")
            Return True
        End Operator
    End Structure

    Sub Main()
        System.Console.WriteLine(New S1?() AndAlso New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?() AndAlso New S1?(New S1()))
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) AndAlso New S1?())
        System.Console.WriteLine("----")
        System.Console.WriteLine(New S1?(New S1()) AndAlso New S1?(New S1()))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
And(x As S1?, y As S1?) As S1?

----
And(x As S1?, y As S1?) As S1?

----
IsFalse(x As S1) As Boolean
And(x As S1?, y As S1?) As S1?
Module1+S1
----
IsFalse(x As S1) As Boolean
And(x As S1?, y As S1?) As S1?
Module1+S1
]]>)
        End Sub

        <Fact>
        Public Sub LateBound1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As S1) As S1
            System.Console.WriteLine("+(x As S1, y As S1) As S1")
            Return x
        End Operator
        Public Shared Operator +(x As S1?, y As S1) As S1
            System.Console.WriteLine("+(x As S1?, y As S1) As S1")
            Return y
        End Operator
    End Structure


    Sub Main()
        Dim x As Object = New S1()
        System.Console.WriteLine(New S1() + x) 'BIND1:"New S1()"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+(x As S1, y As S1) As S1
Module1+S1
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 1)
            Dim typeInfo = model.GetTypeInfo(node)
            Assert.Equal("Module1.S1", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub LateBound2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Interface I1
    End Interface

    Structure S1
        Implements I1

        Public Shared Operator +(x As S1, y As S1) As S1
            System.Console.WriteLine("+(x As S1, y As S1) As S1")
            Return x
        End Operator
    End Structure

    Sub Test(Of T As I1)(x As T, y As Object)
        System.Console.WriteLine(x + y)
        System.Console.WriteLine(y + x)
    End Sub

    Sub Main()
        Test(New S1(), New S1())
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+(x As S1, y As S1) As S1
Module1+S1
+(x As S1, y As S1) As S1
Module1+S1
]]>)
        End Sub

        <Fact>
        Public Sub LateBound3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Interface I1
    End Interface

    Class B2
    End Class

    Class B3
        Inherits B2
        Implements I1

        Public Shared Operator +(x As B3, y As B3) As B3
            System.Console.WriteLine("+(x As B3, y As B3) As B3")
            Return x
        End Operator
    End Class

    Sub Test(Of T As {I1, B2})(x As T, y As Object)
        System.Console.WriteLine(x + y)
        System.Console.WriteLine(y + x)
    End Sub

    Sub Main()
        Test(New B3(), New B3())
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+(x As B3, y As B3) As B3
Module1+B3
+(x As B3, y As B3) As B3
Module1+B3
]]>)

        End Sub

        <Fact>
        Public Sub UndefinedOp1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B3
    End Class

    Sub Main()
        Dim o = New B3() + New B3()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30452: Operator '+' is not defined for types 'Module1.B3' and 'Module1.B3'.
        Dim o = New B3() + New B3()
                ~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Resolution1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Structure S6
        Shared Operator +(x As S6, y As S7) As S7
            Return Nothing
        End Operator
    End Structure
    Structure S7
        Shared Operator +(x As S6, y As S7) As S6
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x2? As S6
        Dim y2? As S7

        Dim r1 = New S6() + New S7()
        Dim r2 = x2 + y2
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible '+' is most specific for these arguments:
    'Public Shared Operator Module1.S6.+(x As Module1.S6, y As Module1.S7) As Module1.S7': Not most specific.
    'Public Shared Operator Module1.S7.+(x As Module1.S6, y As Module1.S7) As Module1.S6': Not most specific.
        Dim r1 = New S6() + New S7()
                 ~~~~~~~~~~~~~~~~~~~
BC30521: Overload resolution failed because no accessible '+' is most specific for these arguments:
    'Public Shared Operator Module1.S6.+(x As Module1.S6, y As Module1.S7) As Module1.S7': Not most specific.
    'Public Shared Operator Module1.S7.+(x As Module1.S6, y As Module1.S7) As Module1.S6': Not most specific.
        Dim r2 = x2 + y2
                 ~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Resolution2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Structure S1(Of T)
        Shared Operator +(x As S1(Of T), y As Integer) As S1(Of T)
            System.Console.WriteLine("+(x As S1(Of T), y As Integer) As S1(Of T)")
            Return Nothing
        End Operator

        Shared Operator +(x As S1(Of T), y As T) As S1(Of T)
            System.Console.WriteLine("+(x As S1(Of T), y As T) As S1(Of T)")
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x1 As New S1(Of Integer)?(New S1(Of Integer)())
        Dim y = x1 + 1 'BIND1:"x1 + 1"
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
+(x As S1(Of T), y As Integer) As S1(Of T)
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Dim symbolInfo = model.GetSymbolInfo(node)
            Assert.Equal("Function Module1.S1(Of T).op_Addition(x As Module1.S1(Of T), y As System.Int32) As Module1.S1(Of T)", symbolInfo.Symbol.OriginalDefinition.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Resolution3()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A17
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A17::.ctor

} // end of class A17

.class public auto ansi beforefieldinit A18
       extends A17
{
  .method public hidebysig specialname static 
          class A18  op_Addition(class A18 x,
                                 class A18[] y) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A18 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "A18 op_Addition(A18 x, A18 [] y)"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A18::op_Addition

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A17::.ctor()
    IL_0006:  ret
  } // end of method A18::.ctor

} // end of class A18
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x As A17() = New A18() {}
        Dim y As New A18()
        Dim z = y + x
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True,
                                                                                   options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
A18 op_Addition(A18 x, A18 [] y)
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'A17()' to 'A18()'.
        Dim z = y + x
                    ~
</expected>)
        End Sub

        <Fact>
        Public Sub Resolution4()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A17
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A17::.ctor

} // end of class A17

.class public auto ansi beforefieldinit A18
       extends A17
{
  .method public hidebysig specialname static 
          class A18  op_Addition(class A18 x,
                                 class A18[] y) cil managed
  {
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A18 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "A18 op_Addition(A18 x, A18 [] y)"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A18::op_Addition

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A17::.ctor()
    IL_0006:  ret
  } // end of method A18::.ctor

} // end of class A18
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x As A17() = New A18() {}
        Dim y As New A18()
        Dim z1 = y + x
        Dim z2 = y + y
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True,
                                                                                   options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30589: Argument cannot match a ParamArray parameter.
        Dim z1 = y + x
                     ~
BC30589: Argument cannot match a ParamArray parameter.
        Dim z2 = y + y
                     ~
</expected>)
        End Sub

        <Fact>
        Public Sub UnwrappingNullable1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As Byte) As S1
            System.Console.WriteLine("+(x As S1, y As Byte) As S1")
            Return x
        End Operator

        Public Shared Operator +(x As Byte, y As S1) As S1
            System.Console.WriteLine("+(x As Byte, y As S1) As S1")
            Return y
        End Operator
    End Structure

    Sub Main()

        Dim x As S1
        Dim y As Integer

        Dim z1 = x + y
        Dim z2 = y + x
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
+(x As S1, y As Byte) As S1
+(x As Byte, y As S1) As S1
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Integer' to 'Byte'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Integer' to 'Byte'.
        Dim z2 = y + x
                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub UnwrappingNullable2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As Byte) As S1
            System.Console.WriteLine("+(x As S1, y As Byte) As S1")
            Return x
        End Operator

        Public Shared Operator +(x As Byte, y As S1) As S1
            System.Console.WriteLine("+(x As Byte, y As S1) As S1")
            Return y
        End Operator
    End Structure

    Sub Main()
        Dim x As S1 = Nothing
        Dim y As Integer? = New Integer?(0)

        Dim z1 = x + y
        Dim z2 = y + x

        System.Console.WriteLine("----")
        y = Nothing

        z1 = x + y
        z2 = y + x
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

#If LIFTED_CONVERSIONS_SUPPORTED Then
            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
<![CDATA[
+(x As S1, y As Byte) As S1
+(x As Byte, y As S1) As S1
----
]]>)

            CompilationUtils.AssertTheseErrors(compilation,
<expected>
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        Dim z2 = y + x
                 ~
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        z1 = x + y
                 ~
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        z2 = y + x
             ~
</expected>)
#Else
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        Dim z2 = y + x
                 ~
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        z1 = x + y
                 ~
BC42016: Implicit conversion from 'Integer?' to 'Byte?'.
        z2 = y + x
             ~
</expected>)
#End If
        End Sub

        <Fact>
        Public Sub UnwrappingNullable3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As Byte) As S1
            System.Console.WriteLine("+(x As S1, y As Byte) As S1")
            Return x
        End Operator

        Public Shared Operator +(x As Byte, y As S1) As S1
            System.Console.WriteLine("+(x As Byte, y As S1) As S1")
            Return y
        End Operator
    End Structure

    Structure S2
        Shared Narrowing Operator CType(x As S2?) As Byte
            System.Console.WriteLine("CType(x As S2?) As Byte")
            Return 0
        End Operator
    End Structure

    Sub Main()

        Dim x As S1
        Dim y As S2?

        Dim z1 = x + y
        Dim z2 = y + x
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
CType(x As S2?) As Byte
+(x As S1, y As Byte) As S1
CType(x As S2?) As Byte
+(x As Byte, y As S1) As S1
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte'.
        Dim z2 = y + x
                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub UnwrappingNullable4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As Byte) As S1
            System.Console.WriteLine("+(x As S1, y As Byte) As S1")
            Return x
        End Operator

        Public Shared Operator +(x As Byte, y As S1) As S1
            System.Console.WriteLine("+(x As Byte, y As S1) As S1")
            Return y
        End Operator
    End Structure

    Structure S2
        Shared Narrowing Operator CType(x As S2) As Byte
            System.Console.WriteLine("CType(x As S2) As Byte")
            Return 0
        End Operator
    End Structure

    Sub Main()
        Dim x As S1
        Dim y As S2?

        Dim z1 = x + y
        Dim z2 = y + x

        System.Console.WriteLine("----")
        y = New S2?(New S2())

        z1 = x + y
        z2 = y + x
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

#If LIFTED_CONVERSIONS_SUPPORTED Then
            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
<![CDATA[
----
CType(x As S2) As Byte
+(x As S1, y As Byte) As S1
CType(x As S2) As Byte
+(x As Byte, y As S1) As S1
]]>)

            CompilationUtils.AssertTheseErrors(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        Dim z2 = y + x
                 ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        z1 = x + y
                 ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        z2 = y + x
             ~
</expected>)
#Else
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        Dim z2 = y + x
                 ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        z1 = x + y
                 ~
BC42016: Implicit conversion from 'Module1.S2?' to 'Byte?'.
        z2 = y + x
             ~
</expected>)
#End If
        End Sub

        <Fact()>
        Public Sub UnwrappingNullable5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure S1
        Public Shared Operator +(x As S1, y As Byte) As S1
            System.Console.WriteLine("+(x As S1, y As Byte) As S1")
            Return x
        End Operator

        Public Shared Operator +(x As Byte, y As S1) As S1
            System.Console.WriteLine("+(x As Byte, y As S1) As S1")
            Return y
        End Operator
    End Structure

    Structure S2
        Shared Narrowing Operator CType(x As S2) As Byte?
            System.Console.WriteLine("CType(x As S2) As Byte?")
            Return Nothing
        End Operator
    End Structure

    Sub Main()

        Dim x As S1
        Dim y As S2

        Dim z1 = x + y
        Dim z2 = y + x
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
CType(x As S2) As Byte?
CType(x As S2) As Byte?
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Module1.S2' to 'Byte?'.
        Dim z1 = x + y
                     ~
BC42016: Implicit conversion from 'Module1.S2' to 'Byte?'.
        Dim z2 = y + x
                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub CompoundAssignment1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure teststr(Of T)
        Public a As String
        Public Shared result As String
        Shared Operator /(ByVal x As teststr(Of T), ByVal y As T) As teststr(Of T)
            System.Console.WriteLine("Binary Divide")
        End Operator
    End Structure

    Sub Main()

        Dim x4 As New teststr(Of Integer)
        x4 /= 1I
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Binary Divide
]]>)

        End Sub

        <Fact>
        Public Sub CompoundAssignment2()
            Dim compilationDef =
<compilation name="CompoundAssignment2">
    <file name="a.vb"><![CDATA[
Imports System

Interface IFoo
    Function F() As String
End Interface

Class Foo
    Implements IFoo

    Public Function F() As String Implements IFoo.F
        Return "A"
    End Function

    Shared Operator &(f As Foo, s As String) As Foo
        Console.WriteLine("&")
        Return f
    End Operator

End Class

Module Module1
    Sub Main()
        Dim x As IFoo = New Foo()
        Dim y As New Foo
        y &= x.F()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[&]]>)

        End Sub

        <Fact>
        Public Sub UnsupportedLiftedOperators1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1

    Structure S1
        Shared Operator +(x As S1, y As S1) As String
            Return Nothing
        End Operator

        Shared Operator -(x As S1, y As S1) As Integer()
            Return Nothing
        End Operator

        Shared Operator Not(y As S1) As Object
            Return Nothing
        End Operator

        Shared Operator And(x As S1, y As S1) As String
            Return Nothing
        End Operator

        Shared Operator IsTrue(x As S1) As Boolean
            Return Nothing
        End Operator

        Shared Operator IsFalse(x As S1) As Boolean
            Return Nothing
        End Operator
    End Structure

    Structure S2
        Shared Operator Or(x As S2, y As S2) As S2
            Return Nothing
        End Operator

        Shared Operator IsTrue(x As S2) As String
            Return Nothing
        End Operator

        Shared Operator IsFalse(x As S2) As String
            Return Nothing
        End Operator
    End Structure

    Sub Main()
        Dim x As S1?
        Dim y As S1?
        Dim r1 = x + y
        Dim r2 = x - y
        Dim r3 = Not y
        Dim r4 = x AndAlso y
    End Sub

    Sub Main1()
        Dim x As S1              ' 1
        Dim y As S1              ' 1
        Dim r1 = x + y           ' 1
        Dim r2 = x - y           ' 1
        Dim r3 = Not y           ' 1
        Dim r4 = x AndAlso y     ' 1
    End Sub

    Sub Main2()
        Dim x As S2?             ' 2
        Dim y As S2?             ' 2
        Dim r4 = x OrElse y      ' 2
    End Sub

    Sub Main3()                  ' 3
        Dim x As S2              ' 3
        Dim y As S2              ' 3
        Dim r4 = x OrElse y      ' 3
    End Sub

    Structure S1(Of S)
        Shared Operator +(x As S1(Of S), y As S1(Of S)) As S
            Return Nothing
        End Operator

        Sub Test(x As S1(Of S)?)
            Dim y = x + x
        End Sub

        Sub Test(u As S1(Of S))
            Dim v = u + u
        End Sub
    End Structure

    Structure S3
        Shared Operator +(x As S3, y As S3) As S3?
            Return Nothing
        End Operator

        Sub Test(x As S3?)
            Dim y = x + x
        End Sub

        Sub Test(u As S3)
            Dim v = u + u
        End Sub
    End Structure

End Module

Module M1
    Structure S1
        Dim x As Integer

        Public Sub New(ByVal x As Integer)
            Me.x = x
        End Sub

        Public Shared Operator -(ByVal x As S1) As String
            Return "hi"
        End Operator
    End Structure

    Sub Main1()
        Dim x As New S1?(New S1(42))
        Dim y = -x

        Console.WriteLine(y)
    End Sub
End Module

    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))


            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33023: Operator 'IsTrue' must have a return type of Boolean.
        Shared Operator IsTrue(x As S2) As String
                        ~~~~~~
BC33023: Operator 'IsFalse' must have a return type of Boolean.
        Shared Operator IsFalse(x As S2) As String
                        ~~~~~~~
BC30452: Operator '+' is not defined for types 'Module1.S1?' and 'Module1.S1?'.
        Dim r1 = x + y
                 ~~~~~
BC30452: Operator '-' is not defined for types 'Module1.S1?' and 'Module1.S1?'.
        Dim r2 = x - y
                 ~~~~~
BC30487: Operator 'Not' is not defined for type 'Module1.S1?'.
        Dim r3 = Not y
                 ~~~~~
BC30452: Operator 'AndAlso' is not defined for types 'Module1.S1?' and 'Module1.S1?'.
        Dim r4 = x AndAlso y
                 ~~~~~~~~~~~
BC33034: Return and parameter types of 'Public Shared Operator And(x As Module1.S1, y As Module1.S1) As String' must be 'Module1.S1' to be used in a 'AndAlso' expression.
        Dim r4 = x AndAlso y     ' 1
                 ~~~~~~~~~~~
BC33035: Type 'Module1.S2?' must define operator 'IsTrue' to be used in a 'OrElse' expression.
        Dim r4 = x OrElse y      ' 2
                 ~~~~~~~~~~
BC33035: Type 'Module1.S2' must define operator 'IsTrue' to be used in a 'OrElse' expression.
        Dim r4 = x OrElse y      ' 3
                 ~~~~~~~~~~
BC30452: Operator '+' is not defined for types 'Module1.S1(Of S)?' and 'Module1.S1(Of S)?'.
            Dim y = x + x
                    ~~~~~
BC30487: Operator '-' is not defined for type 'M1.S1?'.
        Dim y = -x
                ~~
</expected>)
        End Sub

        <WorkItem(545765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545765")>
        <Fact>
        Public Sub Bug14415()
            Dim compilationDef =
<compilation name="CompoundAssignment2">
    <file name="a.vb"><![CDATA[
Imports System

Structure ArgumentType
    Public x As Integer
    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub
    Public Shared Narrowing Operator CType(ByVal x As Narrows) As ArgumentType
        Console.WriteLine("****Executing narrowing conversion Narrows->ArgumentType")
        Return New ArgumentType(x.x)
    End Operator
    Public Shared Operator And(ByVal x As ArgumentType, ByVal y As ArgumentType) As ArgumentType
        Return New ArgumentType(x.x And y.x)
    End Operator
End Structure
Structure Narrows
    Public x As Integer
    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub
End Structure
Module M1
    Function GetNullableValue(ByVal value As ArgumentType?) As String
        If (Not value.HasValue) Then
            Return "nothing"
        Else
            Return value.Value.x.ToString
        End If
    End Function
    Sub OutputEntry(ByVal expr As String, ByVal value As ArgumentType?)
        Console.WriteLine("{0} = {1}", expr, GetNullableValue(value))
    End Sub
    Sub Main()
        Dim a As New Narrows(1)
        Dim h? As ArgumentType = Nothing
        Console.WriteLine()
        OutputEntry("h And a", h And a) 'nothing
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
****Executing narrowing conversion Narrows->ArgumentType
h And a = nothing
]]>)

        End Sub

        <WorkItem(545765, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545765")>
        <Fact>
        Public Sub Bug14415_2()
            Dim compilationDef =
<compilation name="CompoundAssignment2">
    <file name="a.vb"><![CDATA[
Imports System

Structure ArgumentType
    Public x As Integer
    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub
    Public Shared Narrowing Operator CType(ByVal x As Narrows) As ArgumentType
        Console.WriteLine("****Executing narrowing conversion Narrows->ArgumentType")
        Return New ArgumentType(x.x)
    End Operator
    Public Shared Operator And(ByVal x As ArgumentType, ByVal y As ArgumentType) As ArgumentType
        Return New ArgumentType(x.x And y.x)
    End Operator
End Structure
Structure Narrows
    Public x As Integer
    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub
End Structure
Module M1
    Function GetNullableValue(ByVal value As ArgumentType?) As String
        If (Not value.HasValue) Then
            Return "nothing"
        Else
            Return value.Value.x.ToString
        End If
    End Function
    Sub OutputEntry(ByVal expr As String, ByVal value As ArgumentType?)
        Console.WriteLine("{0} = {1}", expr, GetNullableValue(value))
    End Sub
    Sub Main()
        Console.WriteLine()
        OutputEntry("a And h", GetA() And GetH())
    End Sub

    Function GetA() As Narrows
        System.Console.WriteLine("GetA")
        Return New Narrows(1)
    End Function

    Function GetH() As ArgumentType?
        System.Console.WriteLine("GetH")
        Return New ArgumentType()
    End Function
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
GetA
****Executing narrowing conversion Narrows->ArgumentType
GetH
a And h = 0
]]>)

        End Sub

        <WorkItem(546782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546782")>
        <Fact>
        Public Sub Bug16815()
            Dim compilationDef =
<compilation name="Bug16815">
    <file name="a.vb"><![CDATA[
Imports System

Module Module2
    Sub Main()
        Dim x As Date = #1/1/2010#
        Dim y As Date = #1/2/2010#

        System.Console.WriteLine(y - x)

        Dim wtch = DateTime.Now
        Dim z = (DateTime.Now - wtch)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                        options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
1.00:00:00
]]>)

        End Sub

    End Class

End Namespace
