' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

    Public Class UserDefinedUnaryOperators
        Inherits BasicTestBase

        <Fact>
        Public Sub Not1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Class C1
        Public Shared Operator Not(x As C1) As C1
            System.Console.WriteLine("Not(x As C1) As C1")
            Return x
        End Operator
    End Class

    Sub Main()
        Dim x = New C1()
        Dim y = Not x 'BIND1:"Not x"
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
Not(x As C1) As C1
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim not_node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 1)
            Dim symbolInfo = model.GetSymbolInfo(not_node)
            Assert.Equal("Function Module1.C1.op_OnesComplement(x As Module1.C1) As Module1.C1", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal("Public Shared Operator Not(x As Module1.C1) As Module1.C1", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact>
        Public Sub Plus1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Class C1
        Public Shared Operator +(x As C1) As C1
            System.Console.WriteLine("+(x As C1) As C1")
            Return x
        End Operator
    End Class

    Sub Main()
        Dim x = New C1()
        Dim y = +x 'BIND1:"+x"
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+(x As C1) As C1
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim not_node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 1)
            Dim symbolInfo = model.GetSymbolInfo(not_node)
            Assert.Equal("Function Module1.C1.op_UnaryPlus(x As Module1.C1) As Module1.C1", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal("Public Shared Operator +(x As Module1.C1) As Module1.C1", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact>
        Public Sub Minus1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Class C1
        Public Shared Operator -(x As C1) As C1
            System.Console.WriteLine("-(x As C1) As C1")
            Return x
        End Operator
    End Class

    Sub Main()
        Dim x = New C1()
        Dim y = -x 'BIND1:"-x"
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
-(x As C1) As C1
]]>)

            Dim model = GetSemanticModel(verifier.Compilation, "a.vb")

            Dim not_node As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(verifier.Compilation, "a.vb", 1)
            Dim symbolInfo = model.GetSymbolInfo(not_node)
            Assert.Equal("Function Module1.C1.op_UnaryNegation(x As Module1.C1) As Module1.C1", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal("Public Shared Operator -(x As Module1.C1) As Module1.C1", symbolInfo.Symbol.ToDisplayString())
        End Sub

        <Fact>
        Public Sub Minus2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Class C1
    End Class

    Sub Main()
        Dim x = New C1()
        Dim y = -x 'BIND1:"-x"
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30487: Operator '-' is not defined for type 'Module1.C1'.
        Dim y = -x 'BIND1:"-x"
                ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Not2()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_OnesComplement(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_OnesComplement"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_OnesComplement

  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim y1 = Not New A11()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30487: Operator 'Not' is not defined for type 'A11'.
        Dim y1 = Not New A11()
                 ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Not3()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  OP_ONESCOMPLEMENT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_ONESCOMPLEMENT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::OP_ONESCOMPLEMENT

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname static 
          class A11  OP_LOGICALNOT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_LOGICALNOT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim y1 = Not New A11()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
OP_ONESCOMPLEMENT
]]>)
        End Sub

        <Fact>
        Public Sub Not4()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim y1 = Not New A11()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
op_LogicalNot
]]>)
        End Sub

        <Fact>
        Public Sub Not5()

            Dim ilSource =
            <![CDATA[
.class public auto ansi beforefieldinit A11
       extends [mscorlib]System.Object
{

  .method public hidebysig specialname static 
          class A11  op_LogicalNot(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "op_LogicalNot"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname static 
          class A11  OP_LOGICALNOT(class A11 x) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init ([0] class A11 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "OP_LOGICALNOT"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method A11::op_LogicalNot

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A11::.ctor

} // end of class A11
]]>

            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim y1 = Not New A11()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, ilSource.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30487: Operator 'Not' is not defined for type 'A11'.
        Dim y1 = Not New A11()
                 ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Plus2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1

        Public Shared Operator +(x As S1) As S1
            System.Console.WriteLine("+(x As S1) As S1")
            Return x
        End Operator

        Public Shared Operator +(x As S1?) As Integer
            System.Console.WriteLine("+(x As S1?) As Integer")
            Return 0
        End Operator

    End Structure

    Sub Main()
        Dim y1 = +New S1()
        System.Console.WriteLine("-----")
        Dim y2 = +New S1?()
        System.Console.WriteLine("-----")
        Dim y3 = +New S1?(New S1())
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+(x As S1) As S1
-----
+(x As S1?) As Integer
-----
+(x As S1?) As Integer
]]>)
        End Sub

        <Fact>
        Public Sub Plus3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1

        Public Shared Operator +(x As S1?) As Integer
            System.Console.WriteLine("+(x As S1?) As Integer")
            Return 0
        End Operator

        Public Shared Operator +(x As S1) As S1
            System.Console.WriteLine("+(x As S1) As S1")
            Return x
        End Operator

    End Structure

    Sub Main()
        Dim y1 = +New S1()
        System.Console.WriteLine("-----")
        Dim y2 = +New S1?()
        System.Console.WriteLine("-----")
        Dim y3 = +New S1?(New S1())
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
+(x As S1) As S1
-----
+(x As S1?) As Integer
-----
+(x As S1?) As Integer
]]>)
        End Sub

        <Fact>
        Public Sub Plus4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Structure S1

        Public Shared Operator +(x As S1) As S1
            System.Console.WriteLine("+(x As S1) As S1")
            Return x
        End Operator

    End Structure

    Sub Main()
        Dim y2 = +New S1?()
        System.Console.WriteLine("-----")
        Dim y3 = +New S1?(New S1())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
-----
+(x As S1) As S1
]]>)
        End Sub

        <Fact(), WorkItem(544313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544313")>
        Public Sub IsTrueIsFalseInOverloadResolutionTakeWhile()
            Dim compilationDef =
<compilation name="IsTrueIsFalseInOverloadResolutionTakeWhile">
    <file name="a.vb"><![CDATA[
Imports System

Module Program
            Sub Main()

                Dim col4 As New scen4(Of BaseClass)
                col4.test(New BaseClass)

                Dim q4 = From i In col4 Take While New BaseClass Select i
                Console.Writeline(col4.flag) '("TakeWhileBaseClassSelect", col4.flag, "pick widening")
            End Sub

        End Module

        Class BaseClass
            Public Shared Operator IsTrue(ByVal x As BaseClass) As Boolean
                Return True
            End Operator
            Public Shared Operator IsFalse(ByVal x As BaseClass) As Boolean
                Return False
            End Operator
        End Class

        Class HasIsTrue
            Public Shared Operator IsTrue(ByVal x As HasIsTrue) As Boolean
                Return True
            End Operator
            Public Shared Operator IsFalse(ByVal x As HasIsTrue) As Boolean
                Return True
            End Operator
        End Class

        Class scen1
            Public flag As String = ""
            Function [Select](ByVal sel As Func(Of Integer, Integer)) As scen1
                flag &= "S1_Select|"
                Return Me
            End Function
            Function TakeWhile(ByVal arg As Func(Of Integer, HasIsTrue)) As scen1
                flag &= "S1_TakeWhileExpr "
                Return Me
            End Function
            Function TakeWhile(ByVal arg As Func(Of Integer, Boolean)) As scen1
                flag &= "S1_TakeWhileSpecific "
                Return Me
            End Function
        End Class

        Class scen4(Of T As BaseClass)
            Public flag As String = ""
            Function [Select](ByVal sel As Func(Of Integer, Integer)) As scen4(Of T)
                flag &= "S4_Select "
                Return Me
            End Function
            Function TakeWhile(ByVal arg As Func(Of Integer, BaseClass)) As scen4(Of T)
                flag &= "S4_TakeWhileBaseClass "
                Return Me
            End Function

            Sub test(ByVal x As T)
                Dim obj As New scen1
                Dim q1 = From i In obj Take While x Select i

                Console.Writeline(obj.flag) '("TakeWhileSpecificSelect", obj.flag, "should be Take While Base class")
            End Sub

        End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={SystemCoreRef}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
S1_TakeWhileSpecific S1_Select|
S4_TakeWhileBaseClass S4_Select
]]>)

        End Sub

        <WorkItem(56376, "https://github.com/dotnet/roslyn/issues/56376")>
        <Fact>
        Public Sub UserDefinedUnaryOperatorInGenericExpressionTree()
            Dim compilationDef =
<compilation name="OperatorsWithDefaultValuesAreNotBound">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Public Class Program
    Public Shared Sub Main()
        GenericMethod(Of String)()
    End Sub
    
    Private Shared Sub GenericMethod(Of T)()
        Dim func As Func(Of Expression(Of Func(Of C(Of T), C(Of T)))) =
            Function ()
                Return Function(x) +x 
            End Function
            
        func().Compile()(Nothing)
    End Sub
End Class

Class C(Of T)
    Public Shared Operator +(c1 As C(Of T)) As C(Of T)
        Console.Write("Run")
        Return c1
    End Operator
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilation(compilationDef, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics()

            CompileAndVerify(compilation, expectedOutput:="Run")
        End Sub

    End Class

End Namespace

